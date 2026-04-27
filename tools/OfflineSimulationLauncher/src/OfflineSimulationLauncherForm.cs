using System;
using System.Collections.Generic;
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
        private const int BattleSlotCount = 5;
        private const string DefaultInputAssetPath = "Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset";
        private const string DefaultHeroCatalogAssetPath = "Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset";
        private const string UnityHubEditorPath = @"C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe";
        private const string LegacyUnityEditorPath = @"C:\Program Files\Unity 6000.3.13f1\Editor\Unity.exe";

        private readonly string repoRoot;
        private readonly string batchPath;
        private readonly JavaScriptSerializer serializer;
        private readonly List<HeroCatalogEntry> heroCatalogEntries;

        private readonly Label repoRootValueLabel;
        private readonly ComboBox unityExecutableComboBox;
        private readonly Label unityExecutableStatusLabel;
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
        private readonly GroupBox manualSelectionGroupBox;
        private readonly ComboBox[] blueHeroSlotComboBoxes;
        private readonly ComboBox[] redHeroSlotComboBoxes;
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
            heroCatalogEntries = HeroCatalogLoader.LoadHeroEntries(LauncherPaths.ResolveHeroesCsvPath(repoRoot));
            blueHeroSlotComboBoxes = new ComboBox[5];
            redHeroSlotComboBoxes = new ComboBox[5];

            Text = "Fight Offline Simulation Launcher";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 760);
            Size = new Size(1180, 940);

            TableLayoutPanel rootLayout = new TableLayoutPanel();
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.AutoScroll = true;
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 6;
            rootLayout.Padding = new Padding(12);
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(rootLayout);

            GroupBox settingsGroupBox = new GroupBox();
            settingsGroupBox.Text = "运行设置";
            settingsGroupBox.Dock = DockStyle.Top;
            settingsGroupBox.AutoSize = true;
            settingsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            settingsGroupBox.Padding = new Padding(12, 12, 12, 8);
            rootLayout.Controls.Add(settingsGroupBox, 0, 0);

            TableLayoutPanel settingsLayout = new TableLayoutPanel();
            settingsLayout.Dock = DockStyle.Top;
            settingsLayout.AutoSize = true;
            settingsLayout.ColumnCount = 3;
            settingsLayout.RowCount = 8;
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            settingsGroupBox.Controls.Add(settingsLayout);

            AddLabel(settingsLayout, 0, "仓库目录");
            repoRootValueLabel = AddValueLabel(settingsLayout, 0, string.IsNullOrWhiteSpace(repoRoot) ? "未找到仓库根目录" : repoRoot);
            settingsLayout.SetColumnSpan(repoRootValueLabel, 2);

            AddLabel(settingsLayout, 1, "Unity 位置");
            FlowLayoutPanel unityPathPanel = new FlowLayoutPanel();
            unityPathPanel.AutoSize = true;
            unityPathPanel.FlowDirection = FlowDirection.LeftToRight;
            unityPathPanel.WrapContents = false;
            unityPathPanel.Dock = DockStyle.Fill;
            settingsLayout.Controls.Add(unityPathPanel, 1, 1);
            settingsLayout.SetColumnSpan(unityPathPanel, 2);

            unityExecutableComboBox = new ComboBox();
            unityExecutableComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            unityExecutableComboBox.Width = 620;
            unityExecutableComboBox.DisplayMember = "DisplayText";
            unityExecutableComboBox.Items.Add(new UnityExecutableOption("本机 Unity Hub", UnityHubEditorPath));
            unityExecutableComboBox.Items.Add(new UnityExecutableOption("旧电脑路径", LegacyUnityEditorPath));
            unityExecutableComboBox.SelectedIndex = ResolveDefaultUnityExecutableOptionIndex();
            unityExecutableComboBox.SelectedIndexChanged += HandleUnityExecutableChanged;
            unityPathPanel.Controls.Add(unityExecutableComboBox);

            unityExecutableStatusLabel = new Label();
            unityExecutableStatusLabel.AutoSize = true;
            unityExecutableStatusLabel.Margin = new Padding(12, 8, 0, 0);
            unityPathPanel.Controls.Add(unityExecutableStatusLabel);

            AddLabel(settingsLayout, 2, "运行模式");
            FlowLayoutPanel modePanel = new FlowLayoutPanel();
            modePanel.AutoSize = true;
            modePanel.FlowDirection = FlowDirection.LeftToRight;
            modePanel.WrapContents = false;
            modePanel.Dock = DockStyle.Fill;
            settingsLayout.Controls.Add(modePanel, 1, 2);
            settingsLayout.SetColumnSpan(modePanel, 2);

            modeComboBox = new ComboBox();
            modeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            modeComboBox.Width = 180;
            modeComboBox.Items.Add("RandomCatalog");
            modeComboBox.Items.Add("FixedInput");
            modeComboBox.Items.Add("ManualSelection");
            modeComboBox.SelectedIndex = 0;
            modeComboBox.SelectedIndexChanged += HandleModeChanged;
            modePanel.Controls.Add(modeComboBox);

            modeHintLabel = new Label();
            modeHintLabel.AutoSize = true;
            modeHintLabel.Margin = new Padding(12, 8, 0, 0);
            modePanel.Controls.Add(modeHintLabel);

            AddLabel(settingsLayout, 3, "运行次数");
            countNumericUpDown = new NumericUpDown();
            countNumericUpDown.Minimum = 1;
            countNumericUpDown.Maximum = 100000;
            countNumericUpDown.Value = 100;
            countNumericUpDown.Width = 160;
            settingsLayout.Controls.Add(countNumericUpDown, 1, 3);

            AddLabel(settingsLayout, 4, "Seed 起点");
            seedStartNumericUpDown = new NumericUpDown();
            seedStartNumericUpDown.Minimum = 0;
            seedStartNumericUpDown.Maximum = 2147483647;
            seedStartNumericUpDown.Value = 0;
            seedStartNumericUpDown.Width = 160;
            settingsLayout.Controls.Add(seedStartNumericUpDown, 1, 4);

            AddLabel(settingsLayout, 5, "输出文件");
            outputPathTextBox = new TextBox();
            outputPathTextBox.Dock = DockStyle.Fill;
            outputPathTextBox.Text = LauncherPaths.ToDisplayPath(repoRoot, BuildNextOutputPathSuggestion());
            settingsLayout.Controls.Add(outputPathTextBox, 1, 5);

            browseOutputButton = new Button();
            browseOutputButton.Text = "选择...";
            browseOutputButton.AutoSize = true;
            browseOutputButton.Click += HandleBrowseOutputClicked;
            settingsLayout.Controls.Add(browseOutputButton, 2, 5);

            AddLabel(settingsLayout, 6, "输入资产");
            inputAssetPathTextBox = new TextBox();
            inputAssetPathTextBox.Dock = DockStyle.Fill;
            inputAssetPathTextBox.Text = DefaultInputAssetPath;
            settingsLayout.Controls.Add(inputAssetPathTextBox, 1, 6);
            settingsLayout.SetColumnSpan(inputAssetPathTextBox, 2);

            AddLabel(settingsLayout, 7, "英雄池资产");
            heroCatalogAssetPathTextBox = new TextBox();
            heroCatalogAssetPathTextBox.Dock = DockStyle.Fill;
            heroCatalogAssetPathTextBox.Text = DefaultHeroCatalogAssetPath;
            settingsLayout.Controls.Add(heroCatalogAssetPathTextBox, 1, 7);
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

            manualSelectionGroupBox = new GroupBox();
            manualSelectionGroupBox.Text = "手动指定英雄";
            manualSelectionGroupBox.Dock = DockStyle.Top;
            manualSelectionGroupBox.AutoSize = true;
            manualSelectionGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            manualSelectionGroupBox.Padding = new Padding(12);
            rootLayout.Controls.Add(manualSelectionGroupBox, 0, 2);

            TableLayoutPanel manualSelectionLayout = new TableLayoutPanel();
            manualSelectionLayout.Dock = DockStyle.Top;
            manualSelectionLayout.AutoSize = true;
            manualSelectionLayout.ColumnCount = 4;
            manualSelectionLayout.RowCount = 6;
            manualSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            manualSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            manualSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            manualSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            manualSelectionGroupBox.Controls.Add(manualSelectionLayout);

            Label manualHintLabel = new Label();
            manualHintLabel.AutoSize = true;
            manualHintLabel.Text = "可只指定部分槽位，留空的槽位会从英雄池中无放回随机补齐。";
            manualHintLabel.Margin = new Padding(0, 0, 0, 8);
            manualSelectionLayout.Controls.Add(manualHintLabel, 0, 0);
            manualSelectionLayout.SetColumnSpan(manualHintLabel, 4);

            for (int slotIndex = 0; slotIndex < BattleSlotCount; slotIndex++)
            {
                AddManualSlotRow(manualSelectionLayout, slotIndex + 1, slotIndex);
            }

            FlowLayoutPanel actionPanel = new FlowLayoutPanel();
            actionPanel.AutoSize = true;
            actionPanel.Dock = DockStyle.Top;
            actionPanel.FlowDirection = FlowDirection.LeftToRight;
            actionPanel.WrapContents = false;
            actionPanel.Padding = new Padding(0, 8, 0, 0);
            rootLayout.Controls.Add(actionPanel, 0, 3);

            startButton = new Button();
            startButton.AutoSize = true;
            startButton.Text = "开始运行";
            startButton.MinimumSize = new Size(120, 36);
            startButton.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
            startButton.Click += HandleStartClicked;
            actionPanel.Controls.Add(startButton);

            openOutputButton = new Button();
            openOutputButton.AutoSize = true;
            openOutputButton.Text = "打开结果位置";
            openOutputButton.Enabled = false;
            openOutputButton.MinimumSize = new Size(120, 36);
            openOutputButton.Margin = new Padding(12, 3, 0, 3);
            openOutputButton.Click += HandleOpenOutputClicked;
            actionPanel.Controls.Add(openOutputButton);

            GroupBox statusGroupBox = new GroupBox();
            statusGroupBox.Text = "运行状态";
            statusGroupBox.Dock = DockStyle.Top;
            statusGroupBox.AutoSize = true;
            statusGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            statusGroupBox.Padding = new Padding(12);
            rootLayout.Controls.Add(statusGroupBox, 0, 4);

            TableLayoutPanel statusLayout = new TableLayoutPanel();
            statusLayout.Dock = DockStyle.Top;
            statusLayout.AutoSize = true;
            statusLayout.ColumnCount = 1;
            statusLayout.RowCount = 3;
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

            GroupBox logGroupBox = new GroupBox();
            logGroupBox.Text = "运行日志";
            logGroupBox.Dock = DockStyle.Fill;
            logGroupBox.Padding = new Padding(12);
            rootLayout.Controls.Add(logGroupBox, 0, 5);

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
            UpdateUnityExecutableState();
            UpdateRepositoryState();
            AppendLogLine("[launcher] Ready.");
        }

        private void HandleUnityExecutableChanged(object sender, EventArgs e)
        {
            UpdateUnityExecutableState();
        }

        private void HandleModeChanged(object sender, EventArgs e)
        {
            UpdateModeState();
        }

        private void AddManualSlotRow(TableLayoutPanel layout, int rowIndex, int slotIndex)
        {
            Label blueLabel = new Label();
            blueLabel.AutoSize = true;
            blueLabel.Text = "蓝方 " + (slotIndex + 1).ToString(CultureInfo.InvariantCulture);
            blueLabel.Margin = new Padding(0, 6, 12, 0);
            layout.Controls.Add(blueLabel, 0, rowIndex);

            ComboBox blueComboBox = CreateHeroSlotComboBox();
            blueHeroSlotComboBoxes[slotIndex] = blueComboBox;
            layout.Controls.Add(blueComboBox, 1, rowIndex);

            Label redLabel = new Label();
            redLabel.AutoSize = true;
            redLabel.Text = "红方 " + (slotIndex + 1).ToString(CultureInfo.InvariantCulture);
            redLabel.Margin = new Padding(16, 6, 12, 0);
            layout.Controls.Add(redLabel, 2, rowIndex);

            ComboBox redComboBox = CreateHeroSlotComboBox();
            redHeroSlotComboBoxes[slotIndex] = redComboBox;
            layout.Controls.Add(redComboBox, 3, rowIndex);
        }

        private ComboBox CreateHeroSlotComboBox()
        {
            ComboBox comboBox = new ComboBox();
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Dock = DockStyle.Fill;
            comboBox.Width = 320;
            comboBox.DisplayMember = "DisplayText";
            comboBox.Items.Add(new HeroCatalogEntry());
            for (int i = 0; i < heroCatalogEntries.Count; i++)
            {
                comboBox.Items.Add(heroCatalogEntries[i]);
            }

            comboBox.SelectedIndex = 0;
            return comboBox;
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

            string unityExecutablePath = ResolveUnityExecutablePathFromForm();
            if (string.IsNullOrWhiteSpace(unityExecutablePath) || !File.Exists(unityExecutablePath))
            {
                MessageBox.Show(this, "所选 Unity.exe 不存在，请先切换 Unity 位置。", "无法启动", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string outputBasePath = ResolveOutputPathFromForm();
            if (string.IsNullOrWhiteSpace(outputBasePath))
            {
                MessageBox.Show(this, "请先填写输出文件路径。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string outputPath = LauncherPaths.BuildTimestampedOutputPath(outputBasePath, DateTime.Now);
            outputPathTextBox.Text = LauncherPaths.ToDisplayPath(repoRoot, outputPath);

            string inputAssetPath = inputAssetPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputAssetPath))
            {
                MessageBox.Show(this, "请输入 BattleInputConfig 资产路径。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (IsRandomMode() || IsManualSelectionMode())
            {
                string heroCatalogPath = heroCatalogAssetPathTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(heroCatalogPath))
                {
                    MessageBox.Show(this, "当前模式需要填写 HeroCatalog 资产路径。", "参数不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (IsManualSelectionMode())
            {
                if (heroCatalogEntries.Count <= 0)
                {
                    MessageBox.Show(this, "未能从 heroes.csv 读取英雄列表，无法使用手动指定英雄模式。", "英雄列表缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string validationError = ValidateManualSelection();
                if (!string.IsNullOrWhiteSpace(validationError))
                {
                    MessageBox.Show(this, validationError, "手动选人无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            startInfo.EnvironmentVariables["UNITY_EXE"] = unityExecutablePath;

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
                AppendLogLine("[launcher] Unity: " + unityExecutablePath);
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
                    outputPathTextBox.Text = LauncherPaths.ToDisplayPath(repoRoot, BuildNextOutputPathSuggestion());
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

            if (IsRandomMode() || IsManualSelectionMode())
            {
                AppendArgument(builder, "-fightOfflineHeroCatalogAssetPath", heroCatalogAssetPathTextBox.Text.Trim());
            }

            if (IsManualSelectionMode())
            {
                AppendArgument(builder, "-fightOfflineBlueHeroSlots", SerializeManualSlots(blueHeroSlotComboBoxes));
                AppendArgument(builder, "-fightOfflineRedHeroSlots", SerializeManualSlots(redHeroSlotComboBoxes));
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

        private string SerializeManualSlots(ComboBox[] comboBoxes)
        {
            if (comboBoxes == null || comboBoxes.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                HeroCatalogEntry selectedEntry = comboBoxes[i].SelectedItem as HeroCatalogEntry;
                if (selectedEntry != null && !string.IsNullOrWhiteSpace(selectedEntry.HeroId))
                {
                    builder.Append(selectedEntry.HeroId);
                }
            }

            return builder.ToString();
        }

        private string ValidateManualSelection()
        {
            HashSet<string> seenHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string error = ValidateManualSide(blueHeroSlotComboBoxes, "蓝方", seenHeroIds);
            if (!string.IsNullOrWhiteSpace(error))
            {
                return error;
            }

            return ValidateManualSide(redHeroSlotComboBoxes, "红方", seenHeroIds);
        }

        private static string ValidateManualSide(ComboBox[] comboBoxes, string sideLabel, HashSet<string> seenHeroIds)
        {
            if (comboBoxes == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < comboBoxes.Length; i++)
            {
                HeroCatalogEntry selectedEntry = comboBoxes[i].SelectedItem as HeroCatalogEntry;
                if (selectedEntry == null || string.IsNullOrWhiteSpace(selectedEntry.HeroId))
                {
                    continue;
                }

                if (!seenHeroIds.Add(selectedEntry.HeroId))
                {
                    return sideLabel + "第 " + (i + 1).ToString(CultureInfo.InvariantCulture) + " 个槽位选择了重复英雄 [" + selectedEntry.HeroId + "]。";
                }
            }

            return string.Empty;
        }

        private void UpdateModeState()
        {
            bool isRandomMode = IsRandomMode();
            bool isManualSelectionMode = IsManualSelectionMode();
            heroCatalogAssetPathTextBox.Enabled = isRandomMode || isManualSelectionMode;
            manualSelectionGroupBox.Visible = isManualSelectionMode;
            modeHintLabel.Text = isRandomMode
                ? "随机模式会从英雄池无放回抽取 10 个英雄。"
                : isManualSelectionMode
                    ? "手动模式可指定部分槽位，剩余槽位会从英雄池随机补齐。"
                    : "固定模式会直接使用 BattleInputConfig 中已配置的双方阵容。";
        }

        private void UpdateUnityExecutableState()
        {
            string unityExecutablePath = ResolveUnityExecutablePathFromForm();
            if (!string.IsNullOrWhiteSpace(unityExecutablePath) && File.Exists(unityExecutablePath))
            {
                unityExecutableStatusLabel.Text = "已找到";
            }
            else
            {
                unityExecutableStatusLabel.Text = "未找到";
            }

            UpdateRepositoryState();
        }

        private void UpdateRepositoryState()
        {
            bool repositoryReady = !string.IsNullOrWhiteSpace(repoRoot) && File.Exists(batchPath);
            bool unityExecutableReady = IsSelectedUnityExecutableReady();
            startButton.Enabled = repositoryReady && unityExecutableReady;
            if (!repositoryReady)
            {
                statusValueLabel.Text = "未找到仓库根目录或 bat 入口。";
                AppendLogLine("[launcher] Could not resolve repository root or batch entry.");
            }
            else if (!unityExecutableReady)
            {
                statusValueLabel.Text = "所选 Unity.exe 不存在，请切换 Unity 位置。";
            }
        }

        private void SetRunningState(bool isRunning)
        {
            unityExecutableComboBox.Enabled = !isRunning;
            modeComboBox.Enabled = !isRunning;
            countNumericUpDown.Enabled = !isRunning;
            seedStartNumericUpDown.Enabled = !isRunning;
            includeMatchRecordsCheckBox.Enabled = !isRunning;
            exportFullLogsCheckBox.Enabled = !isRunning;
            inputAssetPathTextBox.Enabled = !isRunning;
            heroCatalogAssetPathTextBox.Enabled = !isRunning && (IsRandomMode() || IsManualSelectionMode());
            outputPathTextBox.Enabled = !isRunning;
            browseOutputButton.Enabled = !isRunning;
            manualSelectionGroupBox.Enabled = !isRunning && IsManualSelectionMode();
            startButton.Enabled = !isRunning && !string.IsNullOrWhiteSpace(repoRoot) && File.Exists(batchPath) && IsSelectedUnityExecutableReady();
        }

        private bool IsRandomMode()
        {
            return string.Equals(Convert.ToString(modeComboBox.SelectedItem, CultureInfo.InvariantCulture), "RandomCatalog", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsManualSelectionMode()
        {
            return string.Equals(Convert.ToString(modeComboBox.SelectedItem, CultureInfo.InvariantCulture), "ManualSelection", StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveOutputPathFromForm()
        {
            return LauncherPaths.ResolveUserPath(repoRoot, outputPathTextBox.Text.Trim());
        }

        private string ResolveUnityExecutablePathFromForm()
        {
            UnityExecutableOption option = unityExecutableComboBox.SelectedItem as UnityExecutableOption;
            return option == null ? string.Empty : option.ExecutablePath;
        }

        private bool IsSelectedUnityExecutableReady()
        {
            string unityExecutablePath = ResolveUnityExecutablePathFromForm();
            return !string.IsNullOrWhiteSpace(unityExecutablePath) && File.Exists(unityExecutablePath);
        }

        private int ResolveDefaultUnityExecutableOptionIndex()
        {
            for (int i = 0; i < unityExecutableComboBox.Items.Count; i++)
            {
                UnityExecutableOption option = unityExecutableComboBox.Items[i] as UnityExecutableOption;
                if (option != null && File.Exists(option.ExecutablePath))
                {
                    return i;
                }
            }

            return 0;
        }

        private string BuildNextOutputPathSuggestion()
        {
            string baseOutputPath = ResolveOutputPathFromForm();
            if (string.IsNullOrWhiteSpace(baseOutputPath))
            {
                baseOutputPath = LauncherPaths.ResolveDefaultOutputPath(repoRoot);
            }

            return LauncherPaths.BuildTimestampedOutputPath(baseOutputPath, DateTime.Now);
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

        private sealed class UnityExecutableOption
        {
            public UnityExecutableOption(string label, string executablePath)
            {
                Label = label;
                ExecutablePath = executablePath;
            }

            public string Label { get; private set; }

            public string ExecutablePath { get; private set; }

            public string DisplayText
            {
                get
                {
                    return Label + " - " + ExecutablePath;
                }
            }
        }
    }
}
