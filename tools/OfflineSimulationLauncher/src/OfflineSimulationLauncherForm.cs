using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Fight.Tools.OfflineSimulationLauncher
{
    internal sealed class OfflineSimulationLauncherForm : Form
    {
        private const string DefaultInputAssetPath = "Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset";
        private const string DefaultHeroCatalogAssetPath = "Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset";

        private readonly string repoRoot;
        private readonly string batchPath;
        private readonly JavaScriptSerializer serializer;

        private readonly Label repoRootValueLabel;
        private readonly ComboBox modeComboBox;
        private readonly Label modeHintLabel;
        private readonly NumericUpDown countNumericUpDown;
        private readonly NumericUpDown seedStartNumericUpDown;
        private readonly CheckBox includeMatchRecordsCheckBox;
        private readonly CheckBox exportFullLogsCheckBox;
        private readonly TextBox inputAssetPathTextBox;
        private readonly TextBox heroCatalogAssetPathTextBox;
        private readonly TextBox outputPathTextBox;
        private readonly Button browseOutputButton;
        private readonly Button startButton;
        private readonly Button openOutputButton;
        private readonly Label progressSummaryLabel;
        private readonly ProgressBar progressBar;
        private readonly Label statusValueLabel;
        private readonly TextBox logTextBox;
        private readonly Timer progressTimer;

        private Process runningProcess;
        private string currentProgressPath = string.Empty;
        private string currentOutputPath = string.Empty;
        private LauncherProgressSnapshot latestProgress;

        public OfflineSimulationLauncherForm(string resolvedRepoRoot)
        {
            repoRoot = string.IsNullOrWhiteSpace(resolvedRepoRoot)
                ? string.Empty
                : Path.GetFullPath(resolvedRepoRoot);
            batchPath = LauncherPaths.ResolveBatchPath(repoRoot);
            serializer = new JavaScriptSerializer();

            Text = "Fight Offline Simulation Launcher";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 700);
            Size = new Size(1080, 860);

            TableLayoutPanel rootLayout = new TableLayoutPanel();
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.Padding = new Padding(12);
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(rootLayout);

            GroupBox settingsGroupBox = new GroupBox();
            settingsGroupBox.Text = "运行设置";
            settingsGroupBox.Dock = DockStyle.Top;
            settingsGroupBox.Padding = new Padding(12, 12, 12, 8);
            rootLayout.Controls.Add(settingsGroupBox, 0, 0);

            TableLayoutPanel settingsLayout = new TableLayoutPanel();
            settingsLayout.Dock = DockStyle.Top;
            settingsLayout.AutoSize = true;
            settingsLayout.ColumnCount = 3;
            settingsLayout.RowCount = 7;
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            settingsGroupBox.Controls.Add(settingsLayout);

            AddLabel(settingsLayout, 0, "仓库目录");
            repoRootValueLabel = AddValueLabel(settingsLayout, 0, string.IsNullOrWhiteSpace(repoRoot) ? "未找到仓库根目录" : repoRoot);
            settingsLayout.SetColumnSpan(repoRootValueLabel, 2);

            AddLabel(settingsLayout, 1, "运行模式");
            FlowLayoutPanel modePanel = new FlowLayoutPanel();
            modePanel.AutoSize = true;
            modePanel.FlowDirection = FlowDirection.LeftToRight;
            modePanel.WrapContents = false;
            modePanel.Dock = DockStyle.Fill;
            settingsLayout.Controls.Add(modePanel, 1, 1);
            settingsLayout.SetColumnSpan(modePanel, 2);

            modeComboBox = new ComboBox();
            modeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            modeComboBox.Width = 180;
            modeComboBox.Items.Add("RandomCatalog");
            modeComboBox.Items.Add("FixedInput");
            modeComboBox.SelectedIndex = 0;
            modeComboBox.SelectedIndexChanged += HandleModeChanged;
            modePanel.Controls.Add(modeComboBox);

            modeHintLabel = new Label();
            modeHintLabel.AutoSize = true;
            modeHintLabel.Margin = new Padding(12, 8, 0, 0);
            modePanel.Controls.Add(modeHintLabel);

            AddLabel(settingsLayout, 2, "运行次数");
            countNumericUpDown = new NumericUpDown();
            countNumericUpDown.Minimum = 1;
            countNumericUpDown.Maximum = 100000;
            countNumericUpDown.Value = 100;
            countNumericUpDown.Width = 160;
            settingsLayout.Controls.Add(countNumericUpDown, 1, 2);

            AddLabel(settingsLayout, 3, "Seed 起点");
            seedStartNumericUpDown = new NumericUpDown();
            seedStartNumericUpDown.Minimum = 0;
            seedStartNumericUpDown.Maximum = 2147483647;
            seedStartNumericUpDown.Value = 0;
            seedStartNumericUpDown.Width = 160;
            settingsLayout.Controls.Add(seedStartNumericUpDown, 1, 3);

            AddLabel(settingsLayout, 4, "输出文件");
            outputPathTextBox = new TextBox();
            outputPathTextBox.Dock = DockStyle.Fill;
            outputPathTextBox.Text = LauncherPaths.ToDisplayPath(repoRoot, LauncherPaths.ResolveDefaultOutputPath(repoRoot));
            settingsLayout.Controls.Add(outputPathTextBox, 1, 4);

            browseOutputButton = new Button();
            browseOutputButton.Text = "选择...";
            browseOutputButton.AutoSize = true;
            browseOutputButton.Click += HandleBrowseOutputClicked;
            settingsLayout.Controls.Add(browseOutputButton, 2, 4);

            AddLabel(settingsLayout, 5, "输入资产");
            inputAssetPathTextBox = new TextBox();
            inputAssetPathTextBox.Dock = DockStyle.Fill;
            inputAssetPathTextBox.Text = DefaultInputAssetPath;
            settingsLayout.Controls.Add(inputAssetPathTextBox, 1, 5);
            settingsLayout.SetColumnSpan(inputAssetPathTextBox, 2);

            AddLabel(settingsLayout, 6, "英雄池资产");
            heroCatalogAssetPathTextBox = new TextBox();
            heroCatalogAssetPathTextBox.Dock = DockStyle.Fill;
            heroCatalogAssetPathTextBox.Text = DefaultHeroCatalogAssetPath;
            settingsLayout.Controls.Add(heroCatalogAssetPathTextBox, 1, 6);
            settingsLayout.SetColumnSpan(heroCatalogAssetPathTextBox, 2);

            FlowLayoutPanel optionsPanel = new FlowLayoutPanel();
            optionsPanel.AutoSize = true;
            optionsPanel.Dock = DockStyle.Top;
            optionsPanel.Padding = new Padding(0, 8, 0, 0);
            optionsPanel.FlowDirection = FlowDirection.LeftToRight;
            rootLayout.Controls.Add(optionsPanel, 0, 1);

            includeMatchRecordsCheckBox = new CheckBox();
            includeMatchRecordsCheckBox.AutoSize = true;
            includeMatchRecordsCheckBox.Text = "主结果文件保留每场数据";
            optionsPanel.Controls.Add(includeMatchRecordsCheckBox);

            exportFullLogsCheckBox = new CheckBox();
            exportFullLogsCheckBox.AutoSize = true;
            exportFullLogsCheckBox.Text = "额外导出完整事件日志";
            exportFullLogsCheckBox.Margin = new Padding(16, 3, 0, 3);
            optionsPanel.Controls.Add(exportFullLogsCheckBox);

            GroupBox statusGroupBox = new GroupBox();
            statusGroupBox.Text = "运行状态";
            statusGroupBox.Dock = DockStyle.Top;
            statusGroupBox.Padding = new Padding(12);
            rootLayout.Controls.Add(statusGroupBox, 0, 2);

            TableLayoutPanel statusLayout = new TableLayoutPanel();
            statusLayout.Dock = DockStyle.Top;
            statusLayout.AutoSize = true;
            statusLayout.ColumnCount = 1;
            statusLayout.RowCount = 4;
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusGroupBox.Controls.Add(statusLayout);

            progressSummaryLabel = new Label();
            progressSummaryLabel.AutoSize = true;
            progressSummaryLabel.Font = new Font(Font.FontFamily, 14f, FontStyle.Bold);
            progressSummaryLabel.Text = "尚未开始";
            statusLayout.Controls.Add(progressSummaryLabel, 0, 0);

            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Top;
            progressBar.Minimum = 0;
            progressBar.Maximum = 1;
            progressBar.Value = 0;
            progressBar.Height = 24;
            statusLayout.Controls.Add(progressBar, 0, 1);

            statusValueLabel = new Label();
            statusValueLabel.AutoSize = true;
            statusValueLabel.Margin = new Padding(0, 8, 0, 0);
            statusValueLabel.Text = "等待启动。";
            statusLayout.Controls.Add(statusValueLabel, 0, 2);

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.AutoSize = true;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.WrapContents = false;
            buttonPanel.Margin = new Padding(0, 8, 0, 0);
            statusLayout.Controls.Add(buttonPanel, 0, 3);

            startButton = new Button();
            startButton.AutoSize = true;
            startButton.Text = "开始运行";
            startButton.Click += HandleStartClicked;
            buttonPanel.Controls.Add(startButton);

            openOutputButton = new Button();
            openOutputButton.AutoSize = true;
            openOutputButton.Text = "打开结果位置";
            openOutputButton.Enabled = false;
            openOutputButton.Margin = new Padding(12, 3, 0, 3);
            openOutputButton.Click += HandleOpenOutputClicked;
            buttonPanel.Controls.Add(openOutputButton);

            GroupBox logGroupBox = new GroupBox();
            logGroupBox.Text = "运行日志";
            logGroupBox.Dock = DockStyle.Fill;
            logGroupBox.Padding = new Padding(12);
            rootLayout.Controls.Add(logGroupBox, 0, 3);

            logTextBox = new TextBox();
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Multiline = true;
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Font = new Font("Consolas", 10f);
            logGroupBox.Controls.Add(logTextBox);

            progressTimer = new Timer();
            progressTimer.Interval = 500;
            progressTimer.Tick += HandleProgressTimerTick;

            UpdateModeState();
            UpdateRepositoryState();
            AppendLogLine("[launcher] Ready.");
        }

        private void HandleModeChanged(object sender, EventArgs e)
        {
            UpdateModeState();
        }

        private void HandleBrowseOutputClicked(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON 文件|*.json|所有文件|*.*";
            dialog.Title = "选择离线模拟结果文件";
            dialog.FileName = Path.GetFileName(outputPathTextBox.Text);
            string resolvedOutputPath = ResolveOutputPathFromForm();
            string initialDirectory = LauncherPaths.ResolveOutputDirectory(resolvedOutputPath);
            if (Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                outputPathTextBox.Text = dialog.FileName;
            }

            dialog.Dispose();
        }

        private void HandleOpenOutputClicked(object sender, EventArgs e)
        {
            string resolvedOutputPath = string.IsNullOrWhiteSpace(currentOutputPath)
                ? ResolveOutputPathFromForm()
                : currentOutputPath;
            if (File.Exists(resolvedOutputPath))
            {
                Process.Start("explorer.exe", "/select,\"" + resolvedOutputPath + "\"");
                return;
            }

            string outputDirectory = LauncherPaths.ResolveOutputDirectory(resolvedOutputPath);
            if (Directory.Exists(outputDirectory))
            {
                Process.Start("explorer.exe", "\"" + outputDirectory + "\"");
            }
        }

        private void HandleStartClicked(object sender, EventArgs e)
        {
            if (runningProcess != null && !runningProcess.HasExited)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(repoRoot) || !File.Exists(batchPath))
            {
                MessageBox.Show(this, "未找到 tools\\run_stage01_offline_sim.bat，无法启动。", "无法启动", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string outputPath = ResolveOutputPathFromForm();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                MessageBox.Show(this, "请先填写输出文件路径。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string inputAssetPath = inputAssetPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputAssetPath))
            {
                MessageBox.Show(this, "请输入 BattleInputConfig 资产路径。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IsRandomMode())
            {
                string heroCatalogPath = heroCatalogAssetPathTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(heroCatalogPath))
                {
                    MessageBox.Show(this, "随机模式需要填写 HeroCatalog 资产路径。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            string outputDirectory = LauncherPaths.ResolveOutputDirectory(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            currentOutputPath = outputPath;
            currentProgressPath = LauncherPaths.ResolveProgressPath(repoRoot);
            latestProgress = null;
            if (File.Exists(currentProgressPath))
            {
                File.Delete(currentProgressPath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/d /c \"" + BuildBatchCommandLine(outputPath, currentProgressPath) + "\"";
            startInfo.WorkingDirectory = repoRoot;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            runningProcess = new Process();
            runningProcess.StartInfo = startInfo;
            runningProcess.EnableRaisingEvents = true;
            runningProcess.OutputDataReceived += HandleProcessOutputDataReceived;
            runningProcess.ErrorDataReceived += HandleProcessOutputDataReceived;
            runningProcess.Exited += HandleProcessExited;

            try
            {
                SetRunningState(true);
                progressBar.Maximum = Math.Max(1, Convert.ToInt32(countNumericUpDown.Value));
                progressBar.Value = 0;
                progressSummaryLabel.Text = "准备启动...";
                statusValueLabel.Text = "正在启动 Unity batchmode。";
                AppendLogLine("[launcher] Starting offline simulation.");
                AppendLogLine("[launcher] Output: " + outputPath);
                runningProcess.Start();
                runningProcess.BeginOutputReadLine();
                runningProcess.BeginErrorReadLine();
                progressTimer.Start();
            }
            catch (Exception exception)
            {
                SetRunningState(false);
                AppendLogLine("[launcher] Failed to start process: " + exception.Message);
                MessageBox.Show(this, exception.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            BeginInvoke(new Action<string>(AppendLogLine), e.Data);
        }

        private void HandleProcessExited(object sender, EventArgs e)
        {
            int exitCode = runningProcess != null ? runningProcess.ExitCode : -1;
            BeginInvoke(new Action<int>(HandleProcessExitedOnUiThread), exitCode);
        }

        private void HandleProcessExitedOnUiThread(int exitCode)
        {
            progressTimer.Stop();
            PollProgressFile();
            AppendLogLine("[launcher] Process exited with code " + exitCode.ToString(CultureInfo.InvariantCulture) + ".");
            SetRunningState(false);

            if (exitCode == 0)
            {
                if (latestProgress != null && string.Equals(latestProgress.status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    statusValueLabel.Text = "运行完成。结果已写入 " + NormalizePathForDisplay(latestProgress.outputPath);
                }
                else
                {
                    statusValueLabel.Text = "进程已退出，但未读到最终完成状态。";
                }
            }
            else
            {
                statusValueLabel.Text = "运行失败。请查看下方日志和 Temp/stage01_offline_sim_unity.log。";
            }

            openOutputButton.Enabled = File.Exists(currentOutputPath);
        }

        private void HandleProgressTimerTick(object sender, EventArgs e)
        {
            PollProgressFile();
        }

        private void PollProgressFile()
        {
            if (string.IsNullOrWhiteSpace(currentProgressPath) || !File.Exists(currentProgressPath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(currentProgressPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                LauncherProgressSnapshot snapshot = serializer.Deserialize<LauncherProgressSnapshot>(json);
                if (snapshot == null)
                {
                    return;
                }

                latestProgress = snapshot;
                UpdateProgressUi(snapshot);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void UpdateProgressUi(LauncherProgressSnapshot snapshot)
        {
            int totalMatches = Math.Max(1, snapshot.matchCount);
            int completedMatches = Math.Max(0, Math.Min(totalMatches, snapshot.completedMatchCount));

            progressBar.Maximum = totalMatches;
            progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, completedMatches));

            if (string.Equals(snapshot.status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                progressSummaryLabel.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "已完成 {0}/{1} 场",
                    completedMatches,
                    totalMatches);
            }
            else if (string.Equals(snapshot.status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                progressSummaryLabel.Text = "运行失败";
            }
            else if (snapshot.activeMatchNumber > 0)
            {
                progressSummaryLabel.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "正在第 {0}/{1} 场，已完成 {2} 场",
                    snapshot.activeMatchNumber,
                    totalMatches,
                    completedMatches);
            }
            else
            {
                progressSummaryLabel.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "已完成 {0}/{1} 场",
                    completedMatches,
                    totalMatches);
            }

            string normalizedOutputPath = NormalizePathForDisplay(snapshot.outputPath);
            if (!string.IsNullOrWhiteSpace(normalizedOutputPath))
            {
                currentOutputPath = LauncherPaths.ResolveUserPath(repoRoot, normalizedOutputPath);
            }

            if (!string.IsNullOrWhiteSpace(snapshot.message))
            {
                statusValueLabel.Text = snapshot.message;
            }

            if (string.Equals(snapshot.status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                progressBar.Value = progressBar.Maximum;
            }
        }

        private string BuildBatchCommandLine(string outputPath, string progressPath)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("\"");
            builder.Append(batchPath);
            builder.Append("\"");

            AppendArgument(builder, "-fightOfflineMode", Convert.ToString(modeComboBox.SelectedItem, CultureInfo.InvariantCulture));
            AppendArgument(builder, "-fightOfflineCount", Convert.ToString(countNumericUpDown.Value, CultureInfo.InvariantCulture));
            AppendArgument(builder, "-fightOfflineSeedStart", Convert.ToString(seedStartNumericUpDown.Value, CultureInfo.InvariantCulture));
            AppendArgument(builder, "-fightOfflineInputAssetPath", inputAssetPathTextBox.Text.Trim());
            AppendArgument(builder, "-fightOfflineIncludeMatchRecords", includeMatchRecordsCheckBox.Checked ? "true" : "false");
            AppendArgument(builder, "-fightOfflineExportFullLogs", exportFullLogsCheckBox.Checked ? "true" : "false");
            AppendArgument(builder, "-fightOfflineOutputPath", outputPath);
            AppendArgument(builder, "-fightOfflineProgressPath", progressPath);

            if (IsRandomMode())
            {
                AppendArgument(builder, "-fightOfflineHeroCatalogAssetPath", heroCatalogAssetPathTextBox.Text.Trim());
            }

            return builder.ToString();
        }

        private static void AppendArgument(StringBuilder builder, string argumentName, string argumentValue)
        {
            if (builder == null || string.IsNullOrWhiteSpace(argumentName) || string.IsNullOrWhiteSpace(argumentValue))
            {
                return;
            }

            builder.Append(" ");
            builder.Append(argumentName);
            builder.Append(" ");
            builder.Append("\"");
            builder.Append(argumentValue.Replace("\"", "\"\""));
            builder.Append("\"");
        }

        private void UpdateModeState()
        {
            bool isRandomMode = IsRandomMode();
            heroCatalogAssetPathTextBox.Enabled = isRandomMode;
            modeHintLabel.Text = isRandomMode
                ? "随机模式会从英雄池无放回抽取 10 个英雄。"
                : "固定模式会直接使用 BattleInputConfig 中已配置的双方阵容。";
        }

        private void UpdateRepositoryState()
        {
            bool repositoryReady = !string.IsNullOrWhiteSpace(repoRoot) && File.Exists(batchPath);
            startButton.Enabled = repositoryReady;
            if (!repositoryReady)
            {
                statusValueLabel.Text = "未找到仓库根目录或 bat 入口。";
                AppendLogLine("[launcher] Could not resolve repository root or batch entry.");
            }
        }

        private void SetRunningState(bool isRunning)
        {
            modeComboBox.Enabled = !isRunning;
            countNumericUpDown.Enabled = !isRunning;
            seedStartNumericUpDown.Enabled = !isRunning;
            includeMatchRecordsCheckBox.Enabled = !isRunning;
            exportFullLogsCheckBox.Enabled = !isRunning;
            inputAssetPathTextBox.Enabled = !isRunning;
            heroCatalogAssetPathTextBox.Enabled = !isRunning && IsRandomMode();
            outputPathTextBox.Enabled = !isRunning;
            browseOutputButton.Enabled = !isRunning;
            startButton.Enabled = !isRunning && !string.IsNullOrWhiteSpace(repoRoot) && File.Exists(batchPath);
        }

        private bool IsRandomMode()
        {
            return string.Equals(Convert.ToString(modeComboBox.SelectedItem, CultureInfo.InvariantCulture), "RandomCatalog", StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveOutputPathFromForm()
        {
            return LauncherPaths.ResolveUserPath(repoRoot, outputPathTextBox.Text.Trim());
        }

        private static void AddLabel(TableLayoutPanel layout, int rowIndex, string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Text = text;
            label.Margin = new Padding(0, 8, 12, 0);
            layout.Controls.Add(label, 0, rowIndex);
        }

        private static Label AddValueLabel(TableLayoutPanel layout, int rowIndex, string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Text = text;
            label.Margin = new Padding(0, 8, 0, 0);
            layout.Controls.Add(label, 1, rowIndex);
            return label;
        }

        private void AppendLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            logTextBox.AppendText("[" + timestamp + "] " + line + Environment.NewLine);
        }

        private static string NormalizePathForDisplay(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Replace("/", "\\");
        }
    }
}
