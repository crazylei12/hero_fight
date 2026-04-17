using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fight.Tools.BalanceEditor
{
    internal sealed class CsvTable
    {
        private readonly Dictionary<string, CsvColumn> columnsByKey;
        private readonly List<List<string>> sourceRows;

        private CsvTable(
            string filePath,
            List<List<string>> sourceRows,
            int headerRowIndex,
            List<CsvColumn> columns,
            List<CsvDataRow> dataRows)
        {
            FilePath = filePath;
            this.sourceRows = sourceRows;
            HeaderRowIndex = headerRowIndex;
            Columns = columns;
            DataRows = dataRows;
            columnsByKey = new Dictionary<string, CsvColumn>(StringComparer.OrdinalIgnoreCase);

            foreach (CsvColumn column in columns)
            {
                if (!string.IsNullOrWhiteSpace(column.Key) && !columnsByKey.ContainsKey(column.Key))
                {
                    columnsByKey.Add(column.Key, column);
                }
            }
        }

        public string FilePath { get; private set; }

        public int HeaderRowIndex { get; private set; }

        public IList<CsvColumn> Columns { get; private set; }

        public IList<CsvDataRow> DataRows { get; private set; }

        public static CsvTable Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("CSV 文件路径不能为空。", "filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("找不到 CSV 文件。", filePath);
            }

            List<List<string>> sourceRows = ParseCsv(File.ReadAllText(filePath, Encoding.UTF8));
            if (sourceRows.Count == 0)
            {
                throw new InvalidOperationException(string.Format("CSV 文件为空：{0}", filePath));
            }

            int headerRowIndex = -1;
            List<string> headerRow = null;
            for (int index = 0; index < sourceRows.Count; index++)
            {
                if (IsSkippableRow(sourceRows[index]))
                {
                    continue;
                }

                headerRowIndex = index;
                headerRow = sourceRows[index];
                break;
            }

            if (headerRowIndex < 0 || headerRow == null)
            {
                throw new InvalidOperationException(string.Format("CSV 文件缺少表头：{0}", filePath));
            }

            List<string> descriptionRow = FindDescriptionRow(sourceRows, headerRowIndex);
            List<CsvColumn> columns = new List<CsvColumn>();
            for (int columnIndex = 0; columnIndex < headerRow.Count; columnIndex++)
            {
                string rawHeader = headerRow[columnIndex];
                string key;
                string displayName;
                NormalizeHeader(rawHeader, out key, out displayName);

                string description = descriptionRow != null && columnIndex < descriptionRow.Count
                    ? descriptionRow[columnIndex]
                    : string.Empty;

                columns.Add(new CsvColumn(columnIndex, rawHeader, key, displayName, description));
            }

            List<CsvDataRow> dataRows = new List<CsvDataRow>();
            for (int rowIndex = headerRowIndex + 1; rowIndex < sourceRows.Count; rowIndex++)
            {
                List<string> row = sourceRows[rowIndex];
                if (IsSkippableRow(row))
                {
                    continue;
                }

                dataRows.Add(new CsvDataRow(rowIndex, row));
            }

            return new CsvTable(filePath, sourceRows, headerRowIndex, columns, dataRows);
        }

        public bool TryGetColumn(string key, out CsvColumn column)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                column = null;
                return false;
            }

            return columnsByKey.TryGetValue(key, out column);
        }

        public bool HasColumn(string key)
        {
            return columnsByKey.ContainsKey(key);
        }

        public string GetDisplayName(string key)
        {
            CsvColumn column;
            return TryGetColumn(key, out column) ? column.DisplayName : key;
        }

        public string GetValue(CsvDataRow row, string key)
        {
            CsvColumn column;
            if (!TryGetColumn(key, out column))
            {
                return string.Empty;
            }

            return row.GetCell(column.Index);
        }

        public void SetValue(CsvDataRow row, string key, string value)
        {
            CsvColumn column;
            if (!TryGetColumn(key, out column))
            {
                return;
            }

            row.SetCell(column.Index, value);
        }

        public void Save()
        {
            StringBuilder builder = new StringBuilder();
            for (int rowIndex = 0; rowIndex < sourceRows.Count; rowIndex++)
            {
                builder.Append(BuildCsvLine(sourceRows[rowIndex]));
                if (rowIndex < sourceRows.Count - 1)
                {
                    builder.Append(Environment.NewLine);
                }
            }

            File.WriteAllText(FilePath, builder.ToString(), new UTF8Encoding(true));
        }

        private static List<string> FindDescriptionRow(List<List<string>> sourceRows, int headerRowIndex)
        {
            for (int rowIndex = headerRowIndex - 1; rowIndex >= 0; rowIndex--)
            {
                List<string> row = sourceRows[rowIndex];
                string firstNonEmpty = GetFirstNonEmptyCell(row);
                if (!string.IsNullOrWhiteSpace(firstNonEmpty) &&
                    firstNonEmpty.TrimStart().StartsWith("#说明", StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        private static string GetFirstNonEmptyCell(IList<string> row)
        {
            if (row == null)
            {
                return string.Empty;
            }

            for (int index = 0; index < row.Count; index++)
            {
                string cell = row[index];
                if (!string.IsNullOrWhiteSpace(cell))
                {
                    return cell;
                }
            }

            return string.Empty;
        }

        private static bool IsSkippableRow(IList<string> row)
        {
            if (row == null || row.Count == 0)
            {
                return true;
            }

            string firstNonEmpty = GetFirstNonEmptyCell(row);
            return string.IsNullOrWhiteSpace(firstNonEmpty) ||
                   firstNonEmpty.TrimStart().StartsWith("#", StringComparison.Ordinal);
        }

        private static void NormalizeHeader(string headerCell, out string key, out string displayName)
        {
            if (string.IsNullOrWhiteSpace(headerCell))
            {
                key = string.Empty;
                displayName = string.Empty;
                return;
            }

            string normalized = headerCell.Trim().Trim('\uFEFF');
            int separatorIndex = normalized.IndexOf('|');
            if (separatorIndex >= 0)
            {
                key = normalized.Substring(0, separatorIndex).Trim();
                displayName = normalized.Substring(separatorIndex + 1).Trim();
                return;
            }

            key = normalized;
            displayName = normalized;
        }

        private static List<List<string>> ParseCsv(string content)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> currentRow = new List<string>();
            StringBuilder currentCell = new StringBuilder();
            bool inQuotes = false;

            for (int index = 0; index < content.Length; index++)
            {
                char currentChar = content[index];
                if (inQuotes)
                {
                    if (currentChar == '"')
                    {
                        bool hasEscapedQuote = index + 1 < content.Length && content[index + 1] == '"';
                        if (hasEscapedQuote)
                        {
                            currentCell.Append('"');
                            index++;
                            continue;
                        }

                        inQuotes = false;
                        continue;
                    }

                    currentCell.Append(currentChar);
                    continue;
                }

                switch (currentChar)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        break;
                    case '\r':
                        if (index + 1 < content.Length && content[index + 1] == '\n')
                        {
                            index++;
                        }

                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;
                    case '\n':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;
                    default:
                        currentCell.Append(currentChar);
                        break;
                }
            }

            currentRow.Add(currentCell.ToString());
            if (currentRow.Count > 1 || currentRow[0].Length > 0 || rows.Count == 0)
            {
                rows.Add(currentRow);
            }

            return rows;
        }

        private static string BuildCsvLine(IEnumerable<string> cells)
        {
            return string.Join(",", cells.Select(EscapeCsvCell).ToArray());
        }

        private static string EscapeCsvCell(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!needsQuotes)
            {
                return value;
            }

            return string.Format("\"{0}\"", value.Replace("\"", "\"\""));
        }
    }

    internal sealed class CsvColumn
    {
        public CsvColumn(int index, string rawHeader, string key, string displayName, string description)
        {
            Index = index;
            RawHeader = rawHeader ?? string.Empty;
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public int Index { get; private set; }

        public string RawHeader { get; private set; }

        public string Key { get; private set; }

        public string DisplayName { get; private set; }

        public string Description { get; private set; }
    }

    internal sealed class CsvDataRow
    {
        private readonly List<string> cells;

        public CsvDataRow(int sourceRowIndex, List<string> cells)
        {
            SourceRowIndex = sourceRowIndex;
            this.cells = cells;
        }

        public int SourceRowIndex { get; private set; }

        public string GetCell(int index)
        {
            return index >= 0 && index < cells.Count ? cells[index] : string.Empty;
        }

        public void SetCell(int index, string value)
        {
            while (cells.Count <= index)
            {
                cells.Add(string.Empty);
            }

            cells[index] = value ?? string.Empty;
        }
    }
}
