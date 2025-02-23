using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using System.Windows.Controls;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PRPR_ImportMCObjData
{
    public static class DataLoader
    {
        public static void LoadDataToDataGrid(DataGrid dataGrid, string dataFilePath, string templatePath)
        {
            // Загрузка шаблона атрибутов
            List<string> expectedAttributes = LoadAttributeTemplate(templatePath);

            // Загрузка данных
            DataTable dataTable = LoadDataFile(dataFilePath);

            // Проверка структуры данных
            ValidateDataStructure(dataTable, expectedAttributes);

            // Привязка данных к DataGrid
            dataGrid.ItemsSource = dataTable.DefaultView;
        }

        private static List<string> LoadAttributeTemplate(string templatePath)
        {
            List<string> attributes = new List<string>();

            using (var reader = new StreamReader(templatePath))
            {
                // Пропускаем строку с отображаемыми именами
                reader.ReadLine();

                // Читаем строку с именами атрибутов
                string[] attributeNames = reader.ReadLine()?.Split(';');

                if (attributeNames == null || attributeNames.Length < 2)
                    throw new InvalidDataException("Неверный формат шаблона.");

                // Первый столбец в шаблоне пустой, собираем со второго
                for (int i = 1; i < attributeNames.Length; i++)
                {
                    string attr = attributeNames[i].Trim();
                    if (!string.IsNullOrEmpty(attr))
                        attributes.Add(attr);
                }
            }

            if (attributes.Count == 0)
                throw new InvalidDataException("Шаблон не содержит атрибутов.");

            return attributes;
        }

        private static DataTable LoadDataFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".csv" => LoadCsv(filePath),
                ".xlsx" => LoadXlsx(filePath),
                _ => throw new NotSupportedException("Формат файла не поддерживается.")
            };
        }

        private static DataTable LoadCsv(string filePath)
        {
            DataTable dataTable = new DataTable();

            using (var reader = new StreamReader(filePath))
            {
                // Пропускаем строку с отображаемыми именами
                reader.ReadLine();

                // Читаем имена атрибутов
                string[] attributeNames = reader.ReadLine()?.Split(',');

                if (attributeNames == null || attributeNames.Length == 0)
                    throw new InvalidDataException("Файл данных поврежден.");

                // Создаем столбцы
                foreach (string name in attributeNames)
                {
                    dataTable.Columns.Add(name.Trim());
                }

                // Читаем данные
                while (!reader.EndOfStream)
                {
                    string[] row = reader.ReadLine()?.Split(';');
                    if (row != null && row.Length == attributeNames.Length)
                    {
                        dataTable.Rows.Add(row);
                    }
                }
            }

            return dataTable;
        }

        private static DataTable LoadXlsx(string filePath)
        {
            DataTable dataTable = new DataTable();

            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = doc.WorkbookPart;
                WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                // Пропускаем строку с отображаемыми именами
                var rows = sheetData.Elements<Row>().ToList();

                // Читаем имена атрибутов
                Row attributeRow = rows[1];
                foreach (Cell cell in attributeRow.Elements<Cell>())
                {
                    string columnName = GetCellValue(cell, workbookPart);
                    dataTable.Columns.Add(columnName);
                }

                // Читаем данные
                foreach (Row row in rows.Skip(2))
                {
                    DataRow dataRow = dataTable.NewRow();
                    int i = 0;

                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        dataRow[i++] = GetCellValue(cell, workbookPart);
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }

        private static string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                SharedStringTablePart sstPart = workbookPart.SharedStringTablePart;
                return sstPart.SharedStringTable.ChildElements[int.Parse(cell.InnerText)].InnerText;
            }
            return cell.InnerText;
        }

        private static void ValidateDataStructure(DataTable dataTable, List<string> expectedAttributes)
        {
            // Проверка первого столбца
            if (dataTable.Columns.Count == 0 || dataTable.Columns[0].ColumnName != "pos")
                throw new InvalidDataException("Первый столбец должен быть 'pos'.");

            // Проверка остальных столбцов
            var dataAttributes = dataTable.Columns.Cast<DataColumn>()
                .Skip(1)
                .Select(c => c.ColumnName)
                .ToList();

            if (!expectedAttributes.SequenceEqual(dataAttributes))
                throw new InvalidDataException("Структура данных не соответствует шаблону.");
        }
    }

}
