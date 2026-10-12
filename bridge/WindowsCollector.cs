using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using LibreHardwareMonitor.Hardware;

namespace MotherSys;
public sealed class WindowsCollector : IDisposable {
    string? latest; public string? Latest => Volatile.Read(ref latest);
    readonly string session = Guid.NewGuid().ToString(); long sequence;
    readonly Computer? computer;
    readonly int processors = checked((int)GetActiveProcessorCount(0xffff));
    (ulong idle, ulong total)? cpuPrevious;
    readonly Dictionary<string, (TimeSpan cpu, double at)> previousProcesses = new();
    Dictionary<string, (long down, long up)> previousNetwork = new(); double networkAt;
    object cpu = new { usagePercent = (double?)null, logicalProcessors = (int?)null, temperatureC = (double?)null, clockMHz = (double?)null };
    object ram = new { totalBytes = (ulong?)null, usedBytes = (ulong?)null, availableBytes = (ulong?)null, usagePercent = (double?)null };
    object network = new { downloadBytesPerSecond = (double?)null, uploadBytesPerSecond = (double?)null };
    object processes = new { processCount = (int?)null, threadCount = (int?)null, inaccessibleCount = 0, topCpu = Array.Empty<object>(), topMemory = Array.Empty<object>() };
    object[] gpus = [], disks = [];
    DateTimeOffset coreAt, processAt, sensorsAt, diskAt;
    double? temperature, clock; readonly Stopwatch watch = Stopwatch.StartNew();
    public WindowsCollector(bool sensors) {
        if (sensors) { try { computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true }; computer.Open(); } catch (Exception e) { Console.Error.WriteLine("Sensors unavailable: " + e.GetType().Name); computer?.Close(); computer = null; } }
    }
    static object Section(object data, DateTimeOffset at, int age) => new { sampledAt = at, maxAgeMs = age, data };
    public async Task Run(CancellationToken stop) {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do {
            
            if (sequence % 2 == 0) { ReadProcesses(); processAt = DateTimeOffset.UtcNow; }
            // LHM CPU + GPU share a hardware update, limited to one pass every 2 s.
            if (sequence % 2 == 0) { ReadSensors(); sensorsAt = DateTimeOffset.UtcNow; }
            if (sequence % 30 == 0) { ReadDisks(); diskAt = DateTimeOffset.UtcNow; }
            ReadCpu(); ReadRam(); ReadNetwork(); coreAt = DateTimeOffset.UtcNow;
            var snapshot = new { protocol = "mothersys.telemetry", version = 1, sessionId = session, sequence = sequence++, generatedAt = DateTimeOffset.UtcNow,
                host = new { os = "Windows", name = Environment.MachineName, uptimeSeconds = Environment.TickCount64 / 1000.0 },
                cpu = Section(cpu, coreAt, 4000), ram = Section(ram, coreAt, 4000), network = Section(network, coreAt, 4000),
                gpus = Section(gpus, sensorsAt, 7000), processes = Section(processes, processAt, 7000), disks = Section(disks, diskAt, 65000) };
            Volatile.Write(ref latest, JsonSerializer.Serialize(snapshot));
        } while (await timer.WaitForNextTickAsync(stop));
    }
    void ReadCpu() {
        double? usage = null;
        // GetSystemTimes is primary-group only above 64 processors: decline that case.
        if (processors is > 0 and <= 64 && GetSystemTimes(out var idle, out var kernel, out var user)) {
            var total = kernel + user;
            if (cpuPrevious is { } prev && total > prev.total && idle >= prev.idle) usage = Math.Clamp(100.0 * (1 - (double)(idle - prev.idle) / (total - prev.total)), 0, 100);
            cpuPrevious = (idle, total);
        } else cpuPrevious = null;
        cpu = new { usagePercent = usage, logicalProcessors = processors > 0 ? (int?)processors : null, temperatureC = temperature, clockMHz = clock };
    }
    void ReadRam() {
        var m = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        var ok = GlobalMemoryStatusEx(ref m);
        ram = new { totalBytes = ok ? (ulong?)m.TotalPhys : null, usedBytes = ok ? (ulong?)(m.TotalPhys - m.AvailPhys) : null, availableBytes = ok ? (ulong?)m.AvailPhys : null, usagePercent = ok && m.TotalPhys > 0 ? (double?)(100.0 * (m.TotalPhys - m.AvailPhys) / m.TotalPhys) : null };
    }
    void ReadNetwork() {
        var current = new Dictionary<string, (long down, long up)>(); double? down = null, up = null; var at = watch.Elapsed.TotalSeconds; var complete = true;
        try {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))) {
                try { var stats = nic.GetIPStatistics(); current[nic.Id] = (stats.BytesReceived, stats.BytesSent); } catch { complete = false; }
            }
            if (complete && current.Count > 0 && networkAt > 0 && current.Keys.Order().SequenceEqual(previousNetwork.Keys.Order()) && current.All(kv => kv.Value.down >= previousNetwork[kv.Key].down && kv.Value.up >= previousNetwork[kv.Key].up)) {
                down = current.Sum(kv => (double)(kv.Value.down - previousNetwork[kv.Key].down)) / (at - networkAt);
                up = current.Sum(kv => (double)(kv.Value.up - previousNetwork[kv.Key].up)) / (at - networkAt);
            }
        } catch { complete = false; }
        previousNetwork = complete ? current : new(); networkAt = at;
        network = new { downloadBytesPerSecond = down, uploadBytesPerSecond = up };
    }
    record Proc(int pid, string startTime, string name, double? cpuPercent, long? memoryBytes);
    void ReadProcesses() {
        var rows = new List<Proc>(); var next = new Dictionary<string, (TimeSpan cpu, double at)>(); var at = watch.Elapsed.TotalSeconds; var threads = 0; var inaccessible = 0; var threadsComplete = true;
        Process[] all;
        try { all = Process.GetProcesses(); } catch { processes = new { processCount = (int?)null, threadCount = (int?)null, inaccessibleCount = 0, topCpu = Array.Empty<object>(), topMemory = Array.Empty<object>() }; previousProcesses.Clear(); return; }
        foreach (var p in all) using (p) {
            var blocked = false;
            try { threads += p.Threads.Count; } catch { threadsComplete = false; }
            try {
                var start = p.StartTime.ToUniversalTime().ToString("O"); var key = p.Id + ":" + start; double? percent = null; long? memory = null;
                try { var total = p.TotalProcessorTime; if (processors > 0 && previousProcesses.TryGetValue(key, out var old) && total >= old.cpu && at > old.at) percent = Math.Clamp((total - old.cpu).TotalSeconds / (at - old.at) / processors * 100, 0, 100); next[key] = (total, at); } catch { blocked = true; }
                try { memory = p.WorkingSet64; } catch { blocked = true; }
                rows.Add(new Proc(p.Id, start, p.ProcessName, percent, memory));
            } catch { blocked = true; }
            if (blocked) inaccessible++;
        }
        previousProcesses.Clear(); foreach (var kv in next) previousProcesses[kv.Key] = kv.Value;
        processes = new { processCount = all.Length, threadCount = threadsComplete ? (int?)threads : null, inaccessibleCount = inaccessible, topCpu = rows.Where(p => p.cpuPercent.HasValue).OrderByDescending(p => p.cpuPercent).ThenBy(p => p.pid).Take(6), topMemory = rows.Where(p => p.memoryBytes.HasValue).OrderByDescending(p => p.memoryBytes).ThenBy(p => p.pid).Take(6) };
    }
    void ReadDisks() {
        var rows = new List<object>();
        try { foreach (var d in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed)) { try { if (d.IsReady) rows.Add(new { id = d.Name, name = d.Name, mount = d.RootDirectory.FullName, totalBytes = d.TotalSize, usedBytes = d.TotalSize - d.TotalFreeSpace, freeBytes = d.TotalFreeSpace }); } catch { rows.Add(new { id = d.Name, name = d.Name, mount = d.Name, totalBytes = (long?)null, usedBytes = (long?)null, freeBytes = (long?)null }); } } } catch { }
        disks = rows.ToArray();
    }
    static double? Sensor(IHardware h, SensorType type, params string[] names) {
        // Exact named sensor only; never substitute another load or temperature.
        foreach (var name in names) { var s = h.Sensors.FirstOrDefault(s => s.SensorType == type && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); if (s?.Value is float v && float.IsFinite(v) && v >= 0) return v; }
        return null;
    }
    void ReadSensors() {
        temperature = clock = null; var rows = new List<object>();
        if (computer != null) try {
            foreach (var h in computer.Hardware) {
                try {
                    h.Update();
                    if (h.HardwareType == HardwareType.Cpu) { temperature = Sensor(h, SensorType.Temperature, "CPU Package", "Core (Tctl/Tdie)"); clock = Sensor(h, SensorType.Clock, "CPU Core #1", "CPU Core #1 Thread #1"); }
                    if (h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel) rows.Add(new { id = h.Identifier.ToString(), name = h.Name, usagePercent = Sensor(h, SensorType.Load, "GPU Core", "D3D 3D"), vramUsedBytes = Sensor(h, SensorType.SmallData, "GPU Memory Used") * 1048576, vramTotalBytes = Sensor(h, SensorType.SmallData, "GPU Memory Total") * 1048576, temperatureC = Sensor(h, SensorType.Temperature, "GPU Core"), clockMHz = Sensor(h, SensorType.Clock, "GPU Core"), fanRpm = Sensor(h, SensorType.Fan, "GPU Fan", "GPU Fan #1") });
                } catch { /* A failed device is unavailable in this sample. */ }
            }
        } catch { temperature = clock = null; rows.Clear(); }
        gpus = rows.ToArray();
    }
    public void Dispose() { try { computer?.Close(); } catch { } }
    [DllImport("kernel32.dll")] static extern uint GetActiveProcessorCount(ushort group);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [StructLayout(LayoutKind.Sequential)] struct MemoryStatus { public uint Length, Load; public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual; }
}
