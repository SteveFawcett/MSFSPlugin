using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MSFSPlugin.Classes
{
    public static class NativeLoader
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public static IntPtr LoadEmbeddedDll(string resourceName, string path , string dllFileName)
        {
            string dllPath = Path.Combine( path, dllFileName);

            Debug.WriteLine($"[NativeLoader] Target path: {dllPath}");

            // Load the native DLL
            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to load native DLL '{dllFileName}'. Win32 Error: {error}");
            }

            Debug.WriteLine($"[NativeLoader] LoadLibrary succeeded. Handle: 0x{handle.ToInt64():X}");
            return handle;
        }
    }

    public static class EmbeddedAssemblyLoader
    {
        public static Assembly LoadManagedAssembly(string path, string dll)
        {
            path = Path.Combine(path, dll);
            Debug.WriteLine($"[EmbeddedAssemblyLoader] Loading managed assembly from: {path}");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Assembly file not found at path: {path}");

            return Assembly.LoadFrom(path);
        }
    }
}

