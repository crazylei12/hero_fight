using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Fight.Tools.BalanceEditor
{
    internal sealed class BalanceEditorForm : Form
    {
        private static readonly string[] HeroBaseKeys =
        {
            "maxHealth",
            "attackPower",
            "defense",
            "attackSpeed",
            "moveSpeed",
            "criticalChance",
            "criticalDamageMultiplier",
            "attackRange",
        };

        private static readonly string[] HeroBasicAttackKeys =
        {
            "basicAttackDamageMultiplier",
            "basicAttackRangeOverride",
            "basicAttackProjectileSpeed",
        };

        private static readonly string[] SkillCoreKeys =
        {
            "castRange",
            "areaRadius",
            "cooldownSeconds",
            "minTargetsToCast",
        };

        private static readonly string[] SkillActionKeys =
        {
            "actionSequenceRepeatCount",
            "actionSequenceDurationSeconds",
            "actionSequenceIntervalSeconds",
            "actionSequenceWindupSeconds",
            "actionSequenceRecoverySeconds",
            "actionSequenceTemporaryBasicAttackRangeOverride",
            "actionSequenceTemporarySkillCastRangeOverride",
        };

        private static readonly Regex EffectColumnRegex = new Regex(
            @"^effect(?<effectIndex>\d+)(Label|PowerMultiplier|RadiusOverride|DurationSeconds|TickIntervalSeconds|ForcedMovementDistance|ForcedMovementDurationSeconds|ForcedMovementPeakHeight)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StatusColumnRegex = new Regex(
            @"^effect(?<effectIndex>\d+)Status(?<statusIndex>\d+)(Label|DurationSeconds|Magnitude|TickIntervalSeconds|MaxStacks)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string fixedBalanceFolderPath;
        private readonly Label fixedFolderPathValueLabel;
        private readonly Button reloadButton;
        private readonly Button saveButton;
        private readonly TextBox heroSearchTextBox;
        private readonly ListBox heroListBox;
        private readonly Label selectedHeroTitleLabel;
        private readonly Label selectedHeroMetaLabel;
        private readonly FlowLayoutPanel heroStatsFlowPanel;
        private readonly FlowLayoutPanel heroSkillsFlowPanel;
        private readonly Label statusLabel;

        private readonly List<FieldEditorBinding> heroFieldBindings;
        private readonly List<FieldEditorBinding> skillFieldBindings;

        private CsvTable heroesTable;
        private CsvTable skillsTable;
        private List<HeroEntryViewModel> allHeroes;
        private HeroEntryViewModel currentHero;
        private SkillLayoutInfo skillLayout;
        private bool suppressHeroSelectionChanged;
        private bool isDirty;

        public BalanceEditorForm(string initialFolder)
        {
            Text = "Fight Balance Editor";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 780);
            Size = new Size(1400, 900);
            fixedBalanceFolderPath = string.IsNullOrWhiteSpace(initialFolder)
                ? string.Empty
                : Path.GetFullPath(initialFolder);

            heroFieldBindings = new List<FieldEditorBinding>();
            skillFieldBindings = new List<FieldEditorBinding>();
            allHeroes = new List<HeroEntryViewModel>();

            TableLayoutPanel rootLayout = new TableLayoutPanel();
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 3;
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(rootLayout);

            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Padding = new Padding(12, 12, 12, 8);
            topPanel.AutoSize = true;
            rootLayout.Controls.Add(topPanel, 0, 0);

            TableLayoutPanel topLayout = new TableLayoutPanel();
            topLayout.ColumnCount = 4;
            topLayout.RowCount = 2;
            topLayout.Dock = DockStyle.Top;
            topLayout.AutoSize = true;
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.Controls.Add(topLayout);

            Label folderLabel = new Label();
            folderLabel.Text = "默认表格目录";
            folderLabel.AutoSize = true;
            folderLabel.Margin = new Padding(0, 8, 8, 0);
            topLayout.Controls.Add(folderLabel, 0, 0);

            fixedFolderPathValueLabel = new Label();
            fixedFolderPathValueLabel.Text = string.IsNullOrWhiteSpace(fixedBalanceFolderPath)
                ? "未找到默认目录"
                : fixedBalanceFolderPath;
            fixedFolderPathValueLabel.AutoSize = true;
            fixedFolderPathValueLabel.MaximumSize = new Size(700, 0);
            fixedFolderPathValueLabel.Margin = new Padding(0, 8, 8, 0);
            topLayout.Controls.Add(fixedFolderPathValueLabel, 1, 0);

            reloadButton = new Button();
            reloadButton.Text = "重新载入";
            reloadButton.AutoSize = true;
            reloadButton.Margin = new Padding(0, 2, 8, 0);
            reloadButton.Click += ReloadButton_Click;
            topLayout.Controls.Add(reloadButton, 2, 0);

            saveButton = new Button();
            saveButton.Text = "保存";
            saveButton.AutoSize = true;
            saveButton.Margin = new Padding(0, 2, 0, 0);
            saveButton.Click += SaveButton_Click;
            topLayout.Controls.Add(saveButton, 3, 0);

            Label searchLabel = new Label();
            searchLabel.Text = "搜索英雄";
            searchLabel.AutoSize = true;
            searchLabel.Margin = new Padding(0, 12, 8, 0);
            topLayout.Controls.Add(searchLabel, 0, 1);

            heroSearchTextBox = new TextBox();
            heroSearchTextBox.Dock = DockStyle.Fill;
            heroSearchTextBox.Margin = new Padding(0, 8, 8, 0);
            heroSearchTextBox.TextChanged += HeroSearchTextBox_TextChanged;
            topLayout.Controls.Add(heroSearchTextBox, 1, 1);
            topLayout.SetColumnSpan(heroSearchTextBox, 3);

            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.SplitterDistance = 320;
            rootLayout.Controls.Add(splitContainer, 0, 1);

            heroListBox = new ListBox();
            heroListBox.Dock = DockStyle.Fill;
            heroListBox.Font = new Font(Font.FontFamily, 10f);
            heroListBox.IntegralHeight = false;
            heroListBox.SelectedIndexChanged += HeroListBox_SelectedIndexChanged;
            splitContainer.Panel1.Padding = new Padding(12);
            splitContainer.Panel1.Controls.Add(heroListBox);

            TableLayoutPanel detailLayout = new TableLayoutPanel();
            detailLayout.ColumnCount = 1;
            detailLayout.RowCount = 2;
            detailLayout.Dock = DockStyle.Fill;
            detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            splitContainer.Panel2.Padding = new Padding(12);
            splitContainer.Panel2.Controls.Add(detailLayout);

            Panel heroHeaderPanel = new Panel();
            heroHeaderPanel.Dock = DockStyle.Top;
            heroHeaderPanel.AutoSize = true;
            heroHeaderPanel.Padding = new Padding(0, 0, 0, 8);
            detailLayout.Controls.Add(heroHeaderPanel, 0, 0);

            selectedHeroTitleLabel = new Label();
            selectedHeroTitleLabel.Text = "请选择左侧英雄";
            selectedHeroTitleLabel.AutoSize = true;
            selectedHeroTitleLabel.Font = new Font(Font.FontFamily, 15f, FontStyle.Bold);
            selectedHeroTitleLabel.Dock = DockStyle.Top;
            heroHeaderPanel.Controls.Add(selectedHeroTitleLabel);

            selectedHeroMetaLabel = new Label();
            selectedHeroMetaLabel.Text = string.Empty;
            selectedHeroMetaLabel.AutoSize = true;
            selectedHeroMetaLabel.Dock = DockStyle.Top;
            selectedHeroMetaLabel.Margin = new Padding(0, 8, 0, 0);
            heroHeaderPanel.Controls.Add(selectedHeroMetaLabel);

            TabControl detailTabControl = new TabControl();
            detailTabControl.Dock = DockStyle.Fill;
            detailLayout.Controls.Add(detailTabControl, 0, 1);

            TabPage statsPage = new TabPage("基础属性");
            TabPage skillsPage = new TabPage("技能修改");
            detailTabControl.TabPages.Add(statsPage);
            detailTabControl.TabPages.Add(skillsPage);

            heroStatsFlowPanel = CreateScrollableFlowPanel();
            statsPage.Controls.Add(heroStatsFlowPanel);

            heroSkillsFlowPanel = CreateScrollableFlowPanel();
            skillsPage.Controls.Add(heroSkillsFlowPanel);

            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Bottom;
            statusPanel.Padding = new Padding(12, 0, 12, 12);
            statusPanel.AutoSize = true;
            rootLayout.Controls.Add(statusPanel, 0, 2);

            statusLabel = new Label();
            statusLabel.Text = string.IsNullOrWhiteSpace(fixedBalanceFolderPath)
                ? "未找到 Unity 默认导表目录。"
                : string.Format("固定使用 Unity 默认导表目录：{0}", fixedBalanceFolderPath);
            statusLabel.AutoSize = true;
            statusPanel.Controls.Add(statusLabel);

            if (!string.IsNullOrWhiteSpace(fixedBalanceFolderPath))
            {
                TryLoadTables(false);
            }
        }

        private static FlowLayoutPanel CreateScrollableFlowPanel()
        {
            FlowLayoutPanel flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.AutoScroll = true;
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.WrapContents = false;
            flowPanel.Padding = new Padding(8);
            return flowPanel;
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            if (isDirty)
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "当前还有未保存的修改，重新载入会丢失这些改动。是否继续？",
                    "确认重新载入",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            TryLoadTables(true);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (heroesTable == null || skillsTable == null)
            {
                MessageBox.Show(this, "请先载入 heroes.csv 和 skills.csv。", "尚未载入", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryCommitCurrentEditors(true))
            {
                return;
            }

            try
            {
                heroesTable.Save();
                skillsTable.Save();
                isDirty = false;
                UpdateStatus(string.Format("已保存：{0}", fixedBalanceFolderPath));
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HeroSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            ApplyHeroFilter(currentHero != null ? currentHero.HeroId : string.Empty);
        }

        private void HeroListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressHeroSelectionChanged)
            {
                return;
            }

            HeroEntryViewModel selectedHero = heroListBox.SelectedItem as HeroEntryViewModel;
            if (selectedHero == null)
            {
                return;
            }

            if (currentHero != null &&
                string.Equals(currentHero.HeroId, selectedHero.HeroId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryCommitCurrentEditors(true))
            {
                RestoreCurrentHeroSelection();
                return;
            }

            RenderHero(selectedHero);
        }

        private void RestoreCurrentHeroSelection()
        {
            suppressHeroSelectionChanged = true;
            try
            {
                if (currentHero == null)
                {
                    heroListBox.ClearSelected();
                    return;
                }

                for (int index = 0; index < heroListBox.Items.Count; index++)
                {
                    HeroEntryViewModel item = heroListBox.Items[index] as HeroEntryViewModel;
                    if (item != null &&
                        string.Equals(item.HeroId, currentHero.HeroId, StringComparison.OrdinalIgnoreCase))
                    {
                        heroListBox.SelectedIndex = index;
                        return;
                    }
                }

                heroListBox.ClearSelected();
            }
            finally
            {
                suppressHeroSelectionChanged = false;
            }
        }

        private void TryLoadTables(bool showSuccessMessage)
        {
            try
            {
                string safeFolderPath = fixedBalanceFolderPath;

                if (string.IsNullOrWhiteSpace(safeFolderPath))
                {
                    throw new InvalidOperationException("未找到 Unity 默认导表目录。请把 EXE 放在仓库内，或从仓库路径启动。");
                }

                string heroesFilePath = Path.Combine(safeFolderPath, "heroes.csv");
                string skillsFilePath = Path.Combine(safeFolderPath, "skills.csv");
                if (!File.Exists(heroesFilePath) || !File.Exists(skillsFilePath))
                {
                    throw new FileNotFoundException(
                        string.Format("默认目录中没有找到 heroes.csv 和 skills.csv：{0}\r\n请先在 Unity 里使用默认导出功能生成表格。", safeFolderPath));
                }

                CsvTable loadedHeroesTable = CsvTable.Load(heroesFilePath);
                CsvTable loadedSkillsTable = CsvTable.Load(skillsFilePath);
                SkillLayoutInfo loadedSkillLayout = SkillLayoutInfo.Build(loadedSkillsTable);
                List<HeroEntryViewModel> loadedHeroes = HeroCatalogBuilder.Build(loadedHeroesTable, loadedSkillsTable, safeFolderPath);

                heroesTable = loadedHeroesTable;
                skillsTable = loadedSkillsTable;
                skillLayout = loadedSkillLayout;
                allHeroes = loadedHeroes;
                currentHero = null;
                isDirty = false;
                ApplyHeroFilter(string.Empty);

                if (heroListBox.Items.Count > 0)
                {
                    suppressHeroSelectionChanged = true;
                    try
                    {
                        heroListBox.SelectedIndex = 0;
                    }
                    finally
                    {
                        suppressHeroSelectionChanged = false;
                    }

                    RenderHero(heroListBox.Items[0] as HeroEntryViewModel);
                }
                else
                {
                    ClearDetailPanels();
                }

                UpdateStatus(string.Format("已载入：{0}", safeFolderPath));
                if (showSuccessMessage)
                {
                    MessageBox.Show(this, "表格已重新载入。", "载入完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "载入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyHeroFilter(string preferredHeroId)
        {
            string searchText = heroSearchTextBox.Text == null
                ? string.Empty
                : heroSearchTextBox.Text.Trim().ToLowerInvariant();

            suppressHeroSelectionChanged = true;
            try
            {
                heroListBox.Items.Clear();
                for (int index = 0; index < allHeroes.Count; index++)
                {
                    HeroEntryViewModel hero = allHeroes[index];
                    if (string.IsNullOrWhiteSpace(searchText) || hero.SearchText.Contains(searchText))
                    {
                        heroListBox.Items.Add(hero);
                    }
                }

                if (heroListBox.Items.Count == 0)
                {
                    heroListBox.ClearSelected();
                    return;
                }

                if (string.IsNullOrWhiteSpace(preferredHeroId) && currentHero != null)
                {
                    preferredHeroId = currentHero.HeroId;
                }

                for (int itemIndex = 0; itemIndex < heroListBox.Items.Count; itemIndex++)
                {
                    HeroEntryViewModel item = heroListBox.Items[itemIndex] as HeroEntryViewModel;
                    if (item != null &&
                        !string.IsNullOrWhiteSpace(preferredHeroId) &&
                        string.Equals(item.HeroId, preferredHeroId, StringComparison.OrdinalIgnoreCase))
                    {
                        heroListBox.SelectedIndex = itemIndex;
                        return;
                    }
                }

                heroListBox.ClearSelected();
            }
            finally
            {
                suppressHeroSelectionChanged = false;
            }
        }

        private void RenderHero(HeroEntryViewModel hero)
        {
            currentHero = hero;
            heroFieldBindings.Clear();
            skillFieldBindings.Clear();

            if (hero == null)
            {
                ClearDetailPanels();
                return;
            }

            selectedHeroTitleLabel.Text = hero.ChineseName;
            selectedHeroMetaLabel.Text = string.Format("英文名：{0}    英雄ID：{1}", hero.EnglishName, hero.HeroId);

            heroStatsFlowPanel.SuspendLayout();
            heroSkillsFlowPanel.SuspendLayout();
            heroStatsFlowPanel.Controls.Clear();
            heroSkillsFlowPanel.Controls.Clear();

            heroStatsFlowPanel.Controls.Add(CreateInfoGroup("英雄信息", new[]
            {
                new KeyValuePair<string, string>("中文名", hero.ChineseName),
                new KeyValuePair<string, string>("英文名", hero.EnglishName),
                new KeyValuePair<string, string>("英雄ID", hero.HeroId),
            }));
            AddFieldSection(heroStatsFlowPanel, "基础属性", heroesTable, hero.HeroRow, HeroBaseKeys, heroFieldBindings, false);
            AddFieldSection(heroStatsFlowPanel, "普攻数值", heroesTable, hero.HeroRow, HeroBasicAttackKeys, heroFieldBindings, false);

            if (hero.Skills.Count == 0)
            {
                heroSkillsFlowPanel.Controls.Add(CreateMessageLabel("这个英雄当前没有挂到任何技能行。"));
            }
            else
            {
                for (int skillIndex = 0; skillIndex < hero.Skills.Count; skillIndex++)
                {
                    heroSkillsFlowPanel.Controls.Add(CreateSkillEditorGroup(hero.Skills[skillIndex]));
                }
            }

            heroStatsFlowPanel.ResumeLayout();
            heroSkillsFlowPanel.ResumeLayout();
        }

        private Control CreateSkillEditorGroup(HeroSkillEntryViewModel skill)
        {
            FlowLayoutPanel skillFlow = CreateSectionContentFlow();

            skillFlow.Controls.Add(CreateInfoGroup("技能信息", new[]
            {
                new KeyValuePair<string, string>("技能ID", skill.SkillId),
                new KeyValuePair<string, string>("技能名", skill.DisplayName),
                new KeyValuePair<string, string>("槽位", string.IsNullOrWhiteSpace(skill.SlotLabel) ? skill.SlotTypeValue : skill.SlotLabel),
            }));

            AddFieldSection(skillFlow, "核心数值", skillsTable, skill.SkillRow, SkillCoreKeys, skillFieldBindings, false);
            AddFieldSection(skillFlow, "动作序列", skillsTable, skill.SkillRow, SkillActionKeys, skillFieldBindings, false);

            for (int effectIndex = 0; effectIndex < skillLayout.MaxEffectCount; effectIndex++)
            {
                string effectLabelKey = string.Format("effect{0}Label", effectIndex);
                string effectLabel = skillsTable.GetValue(skill.SkillRow, effectLabelKey);
                string[] effectKeys =
                {
                    string.Format("effect{0}PowerMultiplier", effectIndex),
                    string.Format("effect{0}RadiusOverride", effectIndex),
                    string.Format("effect{0}DurationSeconds", effectIndex),
                    string.Format("effect{0}TickIntervalSeconds", effectIndex),
                    string.Format("effect{0}ForcedMovementDistance", effectIndex),
                    string.Format("effect{0}ForcedMovementDurationSeconds", effectIndex),
                    string.Format("effect{0}ForcedMovementPeakHeight", effectIndex),
                };

                if (HasAnyValue(skillsTable, skill.SkillRow, effectKeys) || !string.IsNullOrWhiteSpace(effectLabel))
                {
                    string effectTitle = string.Format("效果 {0}", effectIndex);
                    if (!string.IsNullOrWhiteSpace(effectLabel))
                    {
                        effectTitle = string.Format("{0} - {1}", effectTitle, effectLabel);
                    }

                    AddFieldSection(skillFlow, effectTitle, skillsTable, skill.SkillRow, effectKeys, skillFieldBindings, true);
                }

                int maxStatusCount = skillLayout.GetMaxStatusCount(effectIndex);
                for (int statusIndex = 0; statusIndex < maxStatusCount; statusIndex++)
                {
                    string statusLabelKey = string.Format("effect{0}Status{1}Label", effectIndex, statusIndex);
                    string statusLabel = skillsTable.GetValue(skill.SkillRow, statusLabelKey);
                    string[] statusKeys =
                    {
                        string.Format("effect{0}Status{1}DurationSeconds", effectIndex, statusIndex),
                        string.Format("effect{0}Status{1}Magnitude", effectIndex, statusIndex),
                        string.Format("effect{0}Status{1}TickIntervalSeconds", effectIndex, statusIndex),
                        string.Format("effect{0}Status{1}MaxStacks", effectIndex, statusIndex),
                    };

                    if (!HasAnyValue(skillsTable, skill.SkillRow, statusKeys) && string.IsNullOrWhiteSpace(statusLabel))
                    {
                        continue;
                    }

                    string statusTitle = string.Format("效果 {0} - 状态 {1}", effectIndex, statusIndex);
                    if (!string.IsNullOrWhiteSpace(statusLabel))
                    {
                        statusTitle = string.Format("{0} - {1}", statusTitle, statusLabel);
                    }

                    AddFieldSection(skillFlow, statusTitle, skillsTable, skill.SkillRow, statusKeys, skillFieldBindings, true);
                }
            }

            return CreateSectionPanel(
                string.Format("{0}  [{1}]", skill.DisplayName, string.IsNullOrWhiteSpace(skill.SlotLabel) ? skill.SlotTypeValue : skill.SlotLabel),
                900,
                skillFlow);
        }

        private static FlowLayoutPanel CreateSectionContentFlow()
        {
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Top;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoSize = true;
            flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flow.AutoScroll = false;
            flow.Margin = Padding.Empty;
            flow.Padding = Padding.Empty;
            return flow;
        }

        private static Control CreateSectionPanel(string title, int width, Control body)
        {
            Panel panel = new Panel();
            panel.Width = width;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.Margin = new Padding(0, 0, 0, 12);
            panel.Padding = new Padding(10);
            panel.BorderStyle = BorderStyle.FixedSingle;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
            titleLabel.Margin = new Padding(0, 0, 0, 8);

            body.Dock = DockStyle.Top;
            body.Margin = Padding.Empty;

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(body, 0, 1);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control CreateInfoGroup(string title, KeyValuePair<string, string>[] items)
        {
            TableLayoutPanel table = CreateTwoColumnTable();

            for (int index = 0; index < items.Length; index++)
            {
                AddReadOnlyRow(table, items[index].Key, items[index].Value);
            }

            return CreateSectionPanel(title, 900, table);
        }

        private static TableLayoutPanel CreateTwoColumnTable()
        {
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.ColumnCount = 2;
            table.RowCount = 0;
            table.Padding = new Padding(8);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            return table;
        }

        private void AddFieldSection(
            FlowLayoutPanel parent,
            string title,
            CsvTable table,
            CsvDataRow row,
            string[] keys,
            List<FieldEditorBinding> bindings,
            bool hideEmptyFields)
        {
            List<string> visibleKeys = new List<string>();
            for (int index = 0; index < keys.Length; index++)
            {
                string key = keys[index];
                if (!table.HasColumn(key))
                {
                    continue;
                }

                string value = table.GetValue(row, key);
                if (hideEmptyFields && string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                visibleKeys.Add(key);
            }

            if (visibleKeys.Count == 0)
            {
                return;
            }

            TableLayoutPanel tableLayout = CreateTwoColumnTable();

            for (int index = 0; index < visibleKeys.Count; index++)
            {
                AddEditableRow(tableLayout, table, row, visibleKeys[index], bindings);
            }

            parent.Controls.Add(CreateSectionPanel(title, 900, tableLayout));
        }

        private void AddEditableRow(
            TableLayoutPanel tableLayout,
            CsvTable table,
            CsvDataRow row,
            string key,
            List<FieldEditorBinding> bindings)
        {
            NumericKind numericKind = GetNumericKind(key);
            string labelText = GetFieldLabel(table, key);
            string value = table.GetValue(row, key);

            int rowIndex = tableLayout.RowCount++;
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Margin = new Padding(0, 8, 8, 0);
            tableLayout.Controls.Add(label, 0, rowIndex);

            TextBox textBox = new TextBox();
            textBox.Text = value;
            textBox.Width = 220;
            textBox.Margin = new Padding(0, 4, 0, 0);
            tableLayout.Controls.Add(textBox, 1, rowIndex);

            bindings.Add(new FieldEditorBinding(table, row, key, numericKind, textBox, labelText));
        }

        private static void AddReadOnlyRow(TableLayoutPanel tableLayout, string labelText, string value)
        {
            int rowIndex = tableLayout.RowCount++;
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            label.Margin = new Padding(0, 8, 8, 0);
            tableLayout.Controls.Add(label, 0, rowIndex);

            TextBox textBox = new TextBox();
            textBox.Text = value ?? string.Empty;
            textBox.ReadOnly = true;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Width = 420;
            textBox.Margin = new Padding(0, 4, 0, 0);
            tableLayout.Controls.Add(textBox, 1, rowIndex);
        }

        private static Control CreateMessageLabel(string message)
        {
            Label label = new Label();
            label.Text = message;
            label.AutoSize = true;
            label.Padding = new Padding(8);
            return label;
        }

        private void ClearDetailPanels()
        {
            currentHero = null;
            heroFieldBindings.Clear();
            skillFieldBindings.Clear();
            selectedHeroTitleLabel.Text = "请选择左侧英雄";
            selectedHeroMetaLabel.Text = string.Empty;
            heroStatsFlowPanel.Controls.Clear();
            heroSkillsFlowPanel.Controls.Clear();
        }

        private bool TryCommitCurrentEditors(bool showMessage)
        {
            if (!TryCommitBindings(heroFieldBindings, showMessage))
            {
                return false;
            }

            return TryCommitBindings(skillFieldBindings, showMessage);
        }

        private bool TryCommitBindings(List<FieldEditorBinding> bindings, bool showMessage)
        {
            for (int index = 0; index < bindings.Count; index++)
            {
                FieldEditorBinding binding = bindings[index];
                string rawText = binding.Editor.Text == null ? string.Empty : binding.Editor.Text.Trim();
                string normalizedValue;
                string errorMessage;
                if (!TryNormalizeNumericValue(rawText, binding.NumericKind, out normalizedValue, out errorMessage))
                {
                    if (showMessage)
                    {
                        MessageBox.Show(
                            this,
                            string.Format("{0}\r\n字段：{1}", errorMessage, binding.LabelText),
                            "数值格式错误",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    binding.Editor.Focus();
                    binding.Editor.SelectAll();
                    return false;
                }

                string existingValue = binding.Table.GetValue(binding.Row, binding.Key);
                if (!string.Equals(existingValue, normalizedValue, StringComparison.Ordinal))
                {
                    binding.Table.SetValue(binding.Row, binding.Key, normalizedValue);
                    binding.Editor.Text = normalizedValue;
                    isDirty = true;
                }
            }

            return true;
        }

        private static bool TryNormalizeNumericValue(
            string rawText,
            NumericKind numericKind,
            out string normalizedValue,
            out string errorMessage)
        {
            normalizedValue = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                errorMessage = "数值不能为空。";
                return false;
            }

            if (numericKind == NumericKind.Integer)
            {
                int integerValue;
                if (int.TryParse(rawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out integerValue) ||
                    int.TryParse(rawText, NumberStyles.Integer, CultureInfo.CurrentCulture, out integerValue))
                {
                    normalizedValue = integerValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                errorMessage = "请输入合法整数。";
                return false;
            }

            double decimalValue;
            if (double.TryParse(rawText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimalValue) ||
                double.TryParse(rawText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out decimalValue))
            {
                normalizedValue = decimalValue.ToString("0.########", CultureInfo.InvariantCulture);
                return true;
            }

            errorMessage = "请输入合法数字。";
            return false;
        }

        private static bool HasAnyValue(CsvTable table, CsvDataRow row, string[] keys)
        {
            for (int index = 0; index < keys.Length; index++)
            {
                string key = keys[index];
                if (table.HasColumn(key) && !string.IsNullOrWhiteSpace(table.GetValue(row, key)))
                {
                    return true;
                }
            }

            return false;
        }

        private static NumericKind GetNumericKind(string key)
        {
            if (string.Equals(key, "minTargetsToCast", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "actionSequenceRepeatCount", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith("MaxStacks", StringComparison.OrdinalIgnoreCase))
            {
                return NumericKind.Integer;
            }

            return NumericKind.Decimal;
        }

        private static string GetFieldLabel(CsvTable table, string key)
        {
            if (key.EndsWith("PowerMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                return "倍率";
            }

            if (key.EndsWith("RadiusOverride", StringComparison.OrdinalIgnoreCase))
            {
                return "半径覆盖";
            }

            if (key.EndsWith("DurationSeconds", StringComparison.OrdinalIgnoreCase))
            {
                return "持续时间";
            }

            if (key.EndsWith("TickIntervalSeconds", StringComparison.OrdinalIgnoreCase))
            {
                return "跳动间隔";
            }

            if (key.EndsWith("ForcedMovementDistance", StringComparison.OrdinalIgnoreCase))
            {
                return "位移距离";
            }

            if (key.EndsWith("ForcedMovementDurationSeconds", StringComparison.OrdinalIgnoreCase))
            {
                return "位移时长";
            }

            if (key.EndsWith("ForcedMovementPeakHeight", StringComparison.OrdinalIgnoreCase))
            {
                return "位移峰值高度";
            }

            if (key.EndsWith("Magnitude", StringComparison.OrdinalIgnoreCase))
            {
                return "强度";
            }

            if (key.EndsWith("MaxStacks", StringComparison.OrdinalIgnoreCase))
            {
                return "最大层数";
            }

            return table.GetDisplayName(key);
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = isDirty
                ? string.Format("{0}    （有未保存修改）", message)
                : message;
        }

        private sealed class FieldEditorBinding
        {
            public FieldEditorBinding(CsvTable table, CsvDataRow row, string key, NumericKind numericKind, TextBox editor, string labelText)
            {
                Table = table;
                Row = row;
                Key = key;
                NumericKind = numericKind;
                Editor = editor;
                LabelText = labelText;
            }

            public CsvTable Table { get; private set; }

            public CsvDataRow Row { get; private set; }

            public string Key { get; private set; }

            public NumericKind NumericKind { get; private set; }

            public TextBox Editor { get; private set; }

            public string LabelText { get; private set; }
        }

        private enum NumericKind
        {
            Integer,
            Decimal,
        }

        private sealed class SkillLayoutInfo
        {
            public SkillLayoutInfo()
            {
                MaxStatusCountsByEffectIndex = new Dictionary<int, int>();
            }

            public int MaxEffectCount { get; set; }

            public Dictionary<int, int> MaxStatusCountsByEffectIndex { get; private set; }

            public int GetMaxStatusCount(int effectIndex)
            {
                int value;
                return MaxStatusCountsByEffectIndex.TryGetValue(effectIndex, out value) ? value : 0;
            }

            public static SkillLayoutInfo Build(CsvTable skillsTable)
            {
                SkillLayoutInfo info = new SkillLayoutInfo();
                for (int index = 0; index < skillsTable.Columns.Count; index++)
                {
                    CsvColumn column = skillsTable.Columns[index];
                    Match effectMatch = EffectColumnRegex.Match(column.Key);
                    if (effectMatch.Success)
                    {
                        int effectIndex = int.Parse(effectMatch.Groups["effectIndex"].Value, CultureInfo.InvariantCulture);
                        info.MaxEffectCount = Math.Max(info.MaxEffectCount, effectIndex + 1);
                    }

                    Match statusMatch = StatusColumnRegex.Match(column.Key);
                    if (!statusMatch.Success)
                    {
                        continue;
                    }

                    int statusEffectIndex = int.Parse(statusMatch.Groups["effectIndex"].Value, CultureInfo.InvariantCulture);
                    int statusIndex = int.Parse(statusMatch.Groups["statusIndex"].Value, CultureInfo.InvariantCulture);
                    info.MaxEffectCount = Math.Max(info.MaxEffectCount, statusEffectIndex + 1);

                    int existingCount;
                    int nextCount = statusIndex + 1;
                    if (info.MaxStatusCountsByEffectIndex.TryGetValue(statusEffectIndex, out existingCount))
                    {
                        if (nextCount > existingCount)
                        {
                            info.MaxStatusCountsByEffectIndex[statusEffectIndex] = nextCount;
                        }
                    }
                    else
                    {
                        info.MaxStatusCountsByEffectIndex.Add(statusEffectIndex, nextCount);
                    }
                }

                return info;
            }
        }
    }
}
