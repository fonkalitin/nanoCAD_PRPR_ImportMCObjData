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
    /// <summary>
    /// Предоставляет функционал для экспорта данных DataGrid в файл формата XLSX
    /// </summary>
    public static class ExportToXLSX
    {
        /// <summary>
        /// Экспортирует данные из DataGrid в XLSX файл с учетом выбранных чекбоксов
        /// </summary>
        /// <param name="dataGrid">Источник данных для экспорта</param>
        /// <param name="exportHeaders">Флаг экспорта заголовков</param>
        /// <param name="filePath">Путь для сохранения файла</param>
        /// <exception cref="InvalidOperationException">Выбрасывается при некорректной структуре DataGrid</exception>
        public static void ExportToXlsx(DataGrid dataGrid, bool exportHeaders, string filePath)
        {
            // Валидация структуры DataGrid
            if (dataGrid.Columns.Count < 2)
                throw new InvalidOperationException("DataGrid должен иметь как минимум два столбца.");

            // Проверка первого столбца на чекбокс
            var checkBoxColumn = dataGrid.Columns[0] as DataGridCheckBoxColumn
                ?? throw new InvalidOperationException("Первый столбец должен быть DataGridCheckBoxColumn.");

            // Получение привязки данных чекбокса
            var checkBoxBinding = checkBoxColumn.Binding as Binding
                ?? throw new InvalidOperationException("Чекбокс столбец должен иметь валидную привязку.");

            string checkBoxPropertyPath = checkBoxBinding.Path.Path;
            var culture = CultureInfo.GetCultureInfo("ru-RU"); // Установка русской культуры

            // Создание документа Excel
            using (var spreadsheet = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                // Базовая структура документа
                var workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook(); // Создание основной книги

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData()); // Добавление листа

                // Настройка структуры листов
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Экспорт данных" // Имя листа
                });

                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                uint rowIndex = 1; // Счетчик строк

                // Экспорт заголовков
                if (exportHeaders)
                {
                    var headerRow = new Row { RowIndex = rowIndex++ };
                    foreach (var column in dataGrid.Columns.Skip(1)) // Пропуск первого столбца
                    {
                        headerRow.Append(new Cell
                        {
                            DataType = CellValues.String,
                            CellValue = new CellValue(column.Header?.ToString()?.Trim() ?? "")
                        });
                    }
                    sheetData.Append(headerRow);
                }

                // Обработка данных
                foreach (var item in dataGrid.Items)
                {
                    // Фильтрация по состоянию чекбокса
                    if (!GetCheckBoxValue(item, checkBoxPropertyPath)) continue;

                    var dataRow = new Row { RowIndex = rowIndex++ };
                    foreach (var column in dataGrid.Columns.Skip(1)) // Пропуск первого столбца
                    {
                        if (column is DataGridBoundColumn boundColumn)
                        {
                            var bindingPath = (boundColumn.Binding as Binding)?.Path.Path;
                            if (bindingPath == null) continue;

                            // Получение значения и создание ячейки
                            var value = ExportToCSV.BindingEvaluator.GetValue(item, bindingPath);
                            var cell = CreateCellWithValue(value, culture);
                            dataRow.Append(cell);
                        }
                    }
                    sheetData.Append(dataRow);
                }

                workbookPart.Workbook.Save(); // Финализация документа
            }

            // Диалог завершения операции
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

        /// <summary>
        /// Создает ячейку Excel с правильным форматированием значения
        /// </summary>
        /// <param name="value">Значение для записи</param>
        /// <param name="culture">Используемая культура</param>
        /// <returns>Форматированная ячейка</returns>
        private static Cell CreateCellWithValue(object value, CultureInfo culture)
        {
            var cell = new Cell();

            // Обработка null-значений
            if (value == null)
            {
                cell.DataType = CellValues.String;
                cell.CellValue = new CellValue("");
                return cell;
            }

            // Определение типа данных
            switch (value)
            {
                case DateTime dt: // Форматирование даты
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(dt.ToOADate().ToString(CultureInfo.InvariantCulture));
                    cell.StyleIndex = 14U; // Применение стиля даты
                    break;

                case double dbl: // Числа с плавающей точкой
                case decimal dec: // Десятичные числа
                case int integer: // Целые числа
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;

                default: // Текстовые и прочие значения
                    var stringValue = Convert.ToString(value, culture)?
                        .Replace("\r", "")
                        .Replace("\n", "")
                        .Trim() ?? "";

                    // Попытка парсинга чисел с учетом культуры
                    if (double.TryParse(stringValue, NumberStyles.Any, culture, out var number))
                    {
                        cell.DataType = CellValues.Number;
                        cell.CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture));
                    }
                    else // Текстовое представление
                    {
                        cell.DataType = CellValues.InlineString;
                        cell.InlineString = new InlineString(new Text(stringValue));
                    }
                    break;
            }

            return cell;
        }

        /// <summary>
        /// Получает значение чекбокса из объекта данных
        /// </summary>
        /// <param name="item">Элемент данных</param>
        /// <param name="propertyPath">Путь к свойству</param>
        /// <returns>Состояние чекбокса</returns>
        private static bool GetCheckBoxValue(object item, string propertyPath)
        {
            var value = ExportToCSV.BindingEvaluator.GetValue(item, propertyPath);
            return value is bool b && b;
        }

        
    }
}