using System;
using System.IO;
using System.Windows.Forms;

namespace Fight.Tools.BalanceEditor
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string initialFolder = args != null && args.Length > 0
                ? args[0]
                : BalanceEditorPathHelper.ResolveDefaultFolder(AppDomain.CurrentDomain.BaseDirectory);

            Application.Run(new BalanceEditorForm(initialFolder));
        }
    }

    internal static class BalanceEditorPathHelper
    {
        public static string ResolveDefaultFolder(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                return string.Empty;
            }

            string directCandidate = Path.GetFullPath(baseDirectory);
            if (ContainsBalanceSheets(directCandidate))
            {
                return directCandidate;
            }

            string currentDirectory = directCandidate;
            for (int depth = 0; depth < 8; depth++)
            {
                string repoCandidate = Path.Combine(currentDirectory, "game", "BalanceSheets", "Stage01");
                if (ContainsBalanceSheets(repoCandidate))
                {
                    return repoCandidate;
                }

                DirectoryInfo parent = Directory.GetParent(currentDirectory);
                if (parent == null)
                {
                    break;
                }

                currentDirectory = parent.FullName;
            }

            return string.Empty;
        }

        private static bool ContainsBalanceSheets(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return false;
            }

            return File.Exists(Path.Combine(folderPath, "heroes.csv")) &&
                   File.Exists(Path.Combine(folderPath, "skills.csv"));
        }
    }
}
