using System;
using System.IO;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleLogExportUtility
    {
        public const string DefaultExportFolderName = "BattleLogs";

        public static bool TryExport(string exportText, string sessionId, out string path, out string errorMessage, string exportFolderName = DefaultExportFolderName)
        {
            path = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(exportText))
            {
                errorMessage = "No battle log available to export.";
                return false;
            }

            try
            {
                var directory = Path.Combine(Application.persistentDataPath, string.IsNullOrWhiteSpace(exportFolderName) ? DefaultExportFolderName : exportFolderName);
                Directory.CreateDirectory(directory);
                var logId = string.IsNullOrWhiteSpace(sessionId)
                    ? DateTime.Now.ToString("yyyyMMdd_HHmmss")
                    : sessionId;
                path = Path.Combine(directory, $"battle_log_{logId}.txt");
                File.WriteAllText(path, exportText);
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }
    }
}
