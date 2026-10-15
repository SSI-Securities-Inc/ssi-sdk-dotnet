using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SsiSdk.Internal;

/// <summary>The OS machine id, read once per process. Sent as <c>deviceId</c> on trading requests.</summary>
internal static class DeviceId
{
    private static readonly Lazy<string> Cached = new(Resolve, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string Get() => Cached.Value;

    private static string Resolve()
    {
        var id = OsMachineId();
        if (!string.IsNullOrWhiteSpace(id)) return id.Trim();
        return MacAddressOrHost();
    }

    private static string? OsMachineId()
    {
        try
        {
            if (OperatingSystem.IsWindows()) return WindowsMachineGuid();
            if (OperatingSystem.IsLinux()) return LinuxMachineId();
            if (OperatingSystem.IsMacOS()) return MacPlatformUuid();
        }
        catch (Exception)
        {
            // fall through to the MAC / host-name fallback
        }
        return null;
    }

    private static string? WindowsMachineGuid()
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        return key?.GetValue("MachineGuid") as string;
    }

    private static string? LinuxMachineId()
    {
        foreach (var path in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
        {
            if (!File.Exists(path)) continue;
            var text = File.ReadAllText(path).Trim();
            if (text.Length > 0) return text;
        }
        return null;
    }

    private static string? MacPlatformUuid()
    {
        var psi = new ProcessStartInfo("/usr/sbin/ioreg")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-rd1");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("IOPlatformExpertDevice");
        using var proc = Process.Start(psi);
        if (proc is null) return null;
        var output = proc.StandardOutput.ReadToEnd();
        if (!proc.WaitForExit(3000)) return null;
        var match = Regex.Match(output, "\"IOPlatformUUID\"\\s*=\\s*\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string MacAddressOrHost()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
                var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length != 6 || bytes.All(b => b == 0)) continue;
                if ((bytes[0] & 0x01) != 0) continue;
                return string.Join(":", bytes.Select(b => b.ToString("x2")));
            }
        }
        catch (Exception)
        {
            // use the host name below
        }
        return Environment.MachineName;
    }
}
