﻿using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static PRPR_ImportMCObjData.KIPdataWindow;

public static class DataLoader
{
    public static void LoadDataToDataGrid(DataGrid dataGrid, string filePath, string templatePath)
    {
        try
        {
            // Загрузка заголовков (первая строка)
            List<string> headers = (List<string>)LoadCsvData(templatePath, mode: 1);

            // Загрузка имен атрибутов (вторая строка)
            List<string> attributes = (List<string>)LoadCsvData(templatePath, mode: 2);

            // Загрузка данных из файла
            DataTable dataTable = LoadDataFile(filePath); //(DataTable)LoadCsvData(filePath, mode: 2);



            // Проверка структуры данных
            ValidateDataStructure(dataTable, attributes);

            // Преобразование DataTable в динамические объекты
            var parameters = ConvertDataTableToDynamicObjects(dataTable, attributes);

            // Обновление столбцов DataGrid
            UpdateDataGridColumns(dataGrid, headers, attributes);

            // Обновление DataGrid
            dataGrid.ItemsSource = parameters;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
        }
    }

    



    private static void UpdateDataGridColumns(DataGrid dataGrid, List<string> headers, List<string> attributes)
    {
        dataGrid.Columns.Clear();

        // Добавляем столбец с чекбоксами
        dataGrid.Columns.Add(new DataGridCheckBoxColumn
        {
            Header = "Выбор",
            Binding = new Binding("IsSelected")
        });

        // Добавляем остальные столбцы
        for (int i = 0; i < attributes.Count; i++)
        {
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = headers[i], // Используем заголовок из первой строки
                Binding = new Binding(attributes[i]) // Используем имя атрибута из второй строки
            });
        }
    }

    public static DataTable LoadDataFile(string filePath)
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

        using (var reader = new StreamReader(filePath, Encoding.GetEncoding("windows-1251")))
        {
            // Пропускаем строку с отображаемыми именами
            reader.ReadLine();

            // Читаем имена атрибутов
            string[] attributeNames = reader.ReadLine()?.Split(';');

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

    public static object LoadCsvData(string filePath, int mode = 0)
    {
        using (var reader = new StreamReader(filePath, Encoding.GetEncoding("windows-1251")))
        {
            // Читаем первую строку (заголовки)
            string[] headers = reader.ReadLine()?.Split(';');

            if (headers == null || headers.Length == 0)
                throw new InvalidDataException("Файл данных поврежден.");

            // Если mode = 1, возвращаем заголовки (со второго столбца)
            if (mode == 1)
            {
                return headers.Skip(1).Select(h => h.Trim()).ToList();
            }

            // Читаем вторую строку (имена атрибутов)
            string[] attributeNames = reader.ReadLine()?.Split(';');

            if (attributeNames == null || attributeNames.Length == 0)
                throw new InvalidDataException("Файл данных поврежден.");

            // Если mode = 2, возвращаем имена атрибутов (со второго столбца)
            if (mode == 2)
            {
                return attributeNames.Skip(1).Select(a => a.Trim()).ToList();
            }

            // Если mode = 3, возвращаем значение из второй строки первого столбца (ячейка A2)
            if (mode == 3)
            {
                // Вторая строка уже прочитана (это attributeNames), поэтому возвращаем первый элемент
                if (attributeNames.Length > 0)
                {
                    return attributeNames[0].Trim();
                }
                throw new InvalidDataException("Файл данных поврежден или не содержит данных.");
            }

            // Если mode = 0, возвращаем всю таблицу целиком
            DataTable dataTable = new DataTable();

            // Создаем столбцы (со второго столбца)
            foreach (string name in attributeNames.Skip(1))
            {
                dataTable.Columns.Add(name.Trim());
            }

            // Читаем данные (начиная с третьей строки)
            while (!reader.EndOfStream)
            {
                string[] row = reader.ReadLine()?.Split(';');
                if (row != null && row.Length == attributeNames.Length)
                {
                    // Пропускаем первый столбец
                    dataTable.Rows.Add(row.Skip(1).ToArray());
                }
            }

            return dataTable;
        }
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
            //.Skip(1)
            .Select(c => c.ColumnName)
            .ToList();

        if (!expectedAttributes.SequenceEqual(dataAttributes))
            throw new InvalidDataException("Структура данных не соответствует шаблону.");
    }

    private static List<dynamic> ConvertDataTableToDynamicObjects(DataTable dataTable, List<string> attributes)
    {
        var dynamicObjects = new List<dynamic>();

        foreach (DataRow row in dataTable.Rows)
        {
            dynamic obj = new ExpandoObject();
            var dict = (IDictionary<string, object>)obj;

            // Добавляем свойство IsSelected
            dict["IsSelected"] = false; // По умолчанию чекбокс не выбран

            // Заполняем остальные свойства
            foreach (var attribute in attributes)
            {
                if (row.Table.Columns.Contains(attribute))
                {
                    dict[attribute] = row[attribute]?.ToString();
                }
            }

            dynamicObjects.Add(obj);
        }

        return dynamicObjects;
    }
}