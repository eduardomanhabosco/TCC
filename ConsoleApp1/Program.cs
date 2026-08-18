using System.Runtime.InteropServices;

partial class Program
{
    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    static void Main()
    {
        IntPtr handle = GetForegroundWindow();
        GetWindowThreadProcessId(handle, out uint pid);

        Console.WriteLine($"Handle: {handle} | PID: {pid}");
        Console.ReadLine();
    }
}