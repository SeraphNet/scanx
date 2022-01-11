using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;


namespace ScanX.InstallHelpers
{
    [RunInstaller(true)]
    public partial class MainInstallerHelper : System.Configuration.Install.Installer
    {
        public MainInstallerHelper()
        {
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            CleanUp();

            base.OnBeforeInstall(savedState);
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {


            string servicePath = Path.Combine(GetPath(), "ScanX.Protocol.exe");


            ServiceHelper.InstallService(servicePath);
            ServiceHelper.StartService();
            base.OnAfterInstall(savedState);

        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {

            ServiceHelper.StopService();

            base.OnBeforeUninstall(savedState);
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            base.OnAfterUninstall(savedState);



            try
            {
                ServiceHelper.DeleteService();
                var path = GetProtocolFolderPath();

                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private string GetPath()
        {
            var result = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return result;
        }
        private string GetProtocolFolderPath()
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var result = Path.Combine(baseDir, "Protocol");

            return result;
        }

        private void CleanUp()
        {
            try
            {
                ServiceHelper.StopService();
                ServiceHelper.DeleteService();
            }
            catch (Exception)
            {
            }

        }
    }
}
