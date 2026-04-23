using System;
using System.Windows.Forms;

namespace Fight.Tools.OfflineSimulationLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string repoRoot = LauncherPaths.ResolveRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
            Application.Run(new OfflineSimulationLauncherForm(repoRoot));
        }
    }
}
