using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;

namespace PRPR_ImportMCObjData
{
    public static class ExportToXLSX
    {
        public static void ExportToExcel(DataGrid dataGrid, bool exportHeaders, string filePath)
        {
            if (dataGrid.Columns.Count < 2)
                throw new InvalidOperationException("DataGrid должен иметь как минимум два столбца.");

            var checkBoxColumn = dataGrid.Columns[0] as DataGridCheckBoxColumn
                ?? throw new InvalidOperationException("Первый столбец должен быть DataGridCheckBoxColumn.");

            var checkBoxBinding = checkBoxColumn.Binding as Binding
                ?? throw new InvalidOperationException("Чекбокс столбец должен иметь валидную привязку.");

            string checkBoxPropertyPath = checkBoxBinding.Path.Path;
            var culture = CultureInfo.GetCultureInfo("ru-RU");

            using (var spreadsheet = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                // Создаем базовую структуру документа
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Экспорт данных"
                });

                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                uint rowIndex = 1;

                if (exportHeaders)
                {
                    // Создаем строку заголовков
                    var headerRow = new Row { RowIndex = rowIndex++ };
                    foreach (var column in dataGrid.Columns.Skip(1))
                    {
                        headerRow.Append(new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(column.Header?.ToString()?.Trim() ?? "")
                        });
                    }
                    sheetData.Append(headerRow);
                }

                // Обрабатываем данные
                foreach (var item in dataGrid.Items)
                {
                    if (!GetCheckBoxValue(item, checkBoxPropertyPath)) continue;

                    var dataRow = new Row { RowIndex = rowIndex++ };
                    foreach (var column in dataGrid.Columns.Skip(1))
                    {
                        if (column is DataGridBoundColumn boundColumn)
                        {
                            var bindingPath = (boundColumn.Binding as Binding)?.Path.Path;
                            if (bindingPath == null) continue;

                            var value = BindingEvaluator.GetValue(item, bindingPath);
                            var cell = CreateCellWithValue(value, culture);
                            dataRow.Append(cell);
                        }
                    }
                    sheetData.Append(dataRow);
                }

                workbookPart.Workbook.Save();
            }

            var result = MessageBox.Show(
                "Экспорт завершен успешно. Открыть файл XLSX сейчас?",
                "Успех",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                });
            }
        }

        private static Cell CreateCellWithValue(object value, CultureInfo culture)
        {
            var cell = new Cell();

            if (value == null)
            {
                cell.DataType = CellValues.String;
                cell.CellValue = new CellValue("");
                return cell;
            }

            switch (value)
            {
                case DateTime dt:
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(dt.ToOADate().ToString(CultureInfo.InvariantCulture));
                    cell.StyleIndex = 14U; // Стиль даты
                    break;

                case double dbl:
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(dbl.ToString(CultureInfo.InvariantCulture));
                    break;

                case decimal dec:
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(dec.ToString(CultureInfo.InvariantCulture));
                    break;

                case int integer:
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(integer.ToString(CultureInfo.InvariantCulture));
                    break;

                default:
                    var stringValue = Convert.ToString(value, culture)?
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Trim() ?? "";

                    // Автоматическая конвертация чисел с запятыми
                    if (double.TryParse(stringValue, NumberStyles.Any, culture, out var number))
                    {
                        cell.DataType = CellValues.Number;
                        cell.CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        cell.DataType = CellValues.InlineString;
                        cell.InlineString = new InlineString(new Text(stringValue));
                    }
                    break;
            }

            return cell;
        }

        private static bool GetCheckBoxValue(object item, string propertyPath)
        {
            var value = BindingEvaluator.GetValue(item, propertyPath);
            return value is bool b && b;
        }

        public static class BindingEvaluator
        {
            public static object GetValue(object source, string propertyPath)
            {
                var binding = new Binding(propertyPath)
                {
                    Source = source,
                    Mode = BindingMode.OneTime
                };

                var dummy = new DummyObject();
                BindingOperations.SetBinding(dummy, DummyObject.ValueProperty, binding);
                return dummy.Value;
            }

            private class DummyObject : DependencyObject
            {
                public static readonly DependencyProperty ValueProperty =
                    DependencyProperty.Register("Value", typeof(object), typeof(DummyObject));

                public object Value
                {
                    get => GetValue(ValueProperty);
                    set => SetValue(ValueProperty, value);
                }
            }
        }
    }
}



