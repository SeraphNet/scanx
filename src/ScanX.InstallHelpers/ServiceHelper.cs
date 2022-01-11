using System.Diagnostics;

namespace ScanX.InstallHelpers
{
    public class ServiceHelper
    {
        private static string _serviceName = "ScanX";

        public static void InstallService(string servicePath)
        {
            using (Process process = new Process())
            {
                ProcessStartInfo startupInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C sc create {_serviceName} binPath=\"{servicePath}\" start=auto",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.StartInfo = startupInfo;

                process.Start();

                process.WaitForExit();

                ProcessStartInfo setDescription = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C sc description {_serviceName} \"Provides Scanning Interface for Trustee Scanning App\""
                };

                process.StartInfo = setDescription;
                process.Start();
                process.WaitForExit();


            }
        }

        public static void StartService()
        {
            using (Process process = new Process())
            {

                ProcessStartInfo startupInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C sc start {_serviceName}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.StartInfo = startupInfo;

                process.Start();

                process.WaitForExit();
            }
        }

        public static void StopService()
        {
            using (Process process = new Process())
            {

                ProcessStartInfo startupInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C net stop {_serviceName}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.StartInfo = startupInfo;

                process.Start();

                process.WaitForExit();

            }
        }

        public static void DeleteService()
        {
            using (Process process = new Process())
            {
                ProcessStartInfo startupInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sc delete \"{_serviceName}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.StartInfo = startupInfo;

                process.Start();

                process.WaitForExit();

            }
        }
    }
}
