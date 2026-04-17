using System;
using System.IO;
using System.Windows.Forms;

namespace Fight.Tools.BalanceEditor
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string initialFolder = BalanceEditorPathHelper.ResolveDefaultFolder(AppDomain.CurrentDomain.BaseDirectory);

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

                if (Directory.Exists(Path.Combine(currentDirectory, "game")))
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

            return Path.Combine(directCandidate, "game", "BalanceSheets", "Stage01");
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
