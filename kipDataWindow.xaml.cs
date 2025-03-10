﻿using DocumentFormat.OpenXml.Bibliography;
using Multicad;
using App = HostMgd.ApplicationServices;
using Db = Teigha.DatabaseServices;
using Ed = HostMgd.EditorInput;
using Rtm = Teigha.Runtime;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Text;
using System.IO;
using static InternalEnums.KipPosProcessMode;
using static Tools.CadCommand.ActiveDocumentHelper;
using System.Data;



namespace PRPR_ImportMCObjData
{
    /// <summary>
    /// Логика взаимодействия для KIPdataWindow.xaml
    /// Это окно агрегации технологических данных по всем КИПиА
    /// </summary>
    public partial class KIPdataWindow : Window
    {

        // Коллекция для хранения данных параметров
        public ObservableCollection<ParameterData> Parameters { get; set; }

        // Получение фактического пути расположения данной сборки dll
        public static string dllFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ActiveDocPath = GetActiveDocumentPathInfo(PathInfoType.DirectoryPath);
        public static string AttributeMappingsFileName = "AttributeMappings.csv";
        public static string AttributeMappingsFilePath = Path.Combine(dllFolder, AttributeMappingsFileName);
        // Имя обьекта в чертеже куда загружать данные (КИП в АТХ)
        public static string KipObjectName = (string)DataLoader.LoadCsvData(AttributeMappingsFilePath, mode: 3);


        public KIPdataWindow()
        {
            InitializeComponent();
            Parameters = new ObservableCollection<ParameterData>();
            PopulateDataGrid2(1);

        }


        /// <summary>
        /// Загружает настройки из CSV файла.
        /// Формат CSV:
        /// Строка 1: [Имя объекта], [DisplayName1], [DisplayName2], ...
        /// Строка 2: [пусто],       [AttributeName1], [AttributeName2], ...
        /// </summary>
        private (string ObjectName, List<AttributeMapping> Mappings) LoadSettingsFromCsv()
        {
            string csvPath = AttributeMappingsFilePath; // Путь к CSV файлу
            var mappings = new List<AttributeMapping>();
            string objectName = string.Empty;

            try
            {
                // Проверяем существование файла
                if (!File.Exists(csvPath))
                {
                    MessageBox.Show($"Файл настроек {csvPath} не найден");
                    return (objectName, mappings);
                }

                // Читаем все строки файла
                var lines = File.ReadAllLines(csvPath, Encoding.GetEncoding("windows-1251"));

                // Проверяем минимальное требование - 2 строки
                if (lines.Length < 2)
                {
                    MessageBox.Show("Некорректный формат CSV файла");
                    return (objectName, mappings);
                }

                // Разбиваем строки на колонки
                var header = lines[0].Split(';'); // Первая строка: имя объекта и DisplayName
                var attributes = lines[1].Split(';'); // Вторая строка: пусто и AttributeName

                // Имя объекта берется из первой ячейки первой строки
                objectName = header[0].Trim();

                // Обрабатываем колонки начиная с индекса 1
                for (int i = 1; i < Math.Min(header.Length, attributes.Length); i++)
                {
                    // Проверяем, что значения не пустые
                    if (!string.IsNullOrWhiteSpace(header[i]) && !string.IsNullOrWhiteSpace(attributes[i]))
                    {
                        mappings.Add(new AttributeMapping
                        {
                            DisplayName = header[i].Trim(), // DisplayName из первой строки
                            AttributeName = attributes[i].Trim() // AttributeName из второй строки
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}");
            }

            return (objectName, mappings);
        }

        /// <summary>
        /// Заполняет DataGrid данными.
        /// </summary>
        private void PopulateDataGrid()
        {
            
            // Загружаем настройки из CSV
            var (objectName, mappings) = LoadSettingsFromCsv();

            // Проверяем, что настройки загружены
            if (string.IsNullOrEmpty(objectName) || mappings.Count == 0)
            {
                MessageBox.Show("Ошибка загрузки настроек");
                return;
            }

            // Получаем список имен атрибутов для запроса данных
            var attributesList = mappings.Select(m => m.AttributeName).ToList();

            // Получаем данные через метод
            var data = PRPR_METHODS.PRPR_METHODS.CollectParObjData(objectName, attributesList, 0);

            // Проверяем, что данные получены
            if (data == null || data.Count == 0)
            {
                //MessageBox.Show("Нет данных для отображения");

                NoDataBanner.Visibility = Visibility.Visible;
                return;
            }
                        

            // Наполняем коллекцию Parameters
            foreach (var attributeValues in data)
            {
                var parameterData = new ParameterData();

                // Заполняем значения атрибутов
                for (int i = 0; i < mappings.Count; i++)
                {
                    parameterData.Attributes[mappings[i].AttributeName] =
                        attributeValues[i].Value?.ToString() ?? string.Empty;
                }

                Parameters.Add(parameterData);
            }

            // Устанавливаем источник данных для DataGrid
            dataGrid.ItemsSource = Parameters;

            // Создаем динамические столбцы
            CreateDynamicColumns(mappings);
            NoDataBanner.Visibility = Visibility.Collapsed;
        }





        private void PopulateDataGrid2(int mode, string filePath = null)
        {
            List<List<dynamic>> data = null;
            List<string> mappings = null;

            // Режим 1: Используем PRPR_METHODS.CollectParObjData
            if (mode == 1)
            {
                // Загружаем настройки из CSV
                var (objectName, loadedMappings) = LoadSettingsFromCsv();

                // Проверяем, что настройки загружены
                if (string.IsNullOrEmpty(objectName) || loadedMappings.Count == 0)
                {
                    MessageBox.Show("Ошибка загрузки настроек");
                    return;
                }

                // Сохраняем mappings для дальнейшего использования
                mappings = loadedMappings.Select(m => m.AttributeName).ToList();

                // Получаем данные через метод
                List<List<ExValue>> rawData = PRPR_METHODS.PRPR_METHODS.CollectParObjData(objectName, mappings, 0);

                // Преобразуем List<List<ExValue>> в List<List<dynamic>>
                data = new List<List<dynamic>>();
                foreach (var row in rawData)
                {
                    var rowData = new List<dynamic>();
                    foreach (var exValue in row)
                    {
                        rowData.Add(exValue.Value); // Предполагаем, что ExValue имеет свойство Value
                    }
                    data.Add(rowData);
                }
            }
            // Режим 2: Используем LoadDataFile
            else if (mode == 2)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("Не указан путь к файлу для режима 2");
                    return;
                }

                // Загружаем данные из файла
                DataTable dataTable = DataLoader.LoadDataFile(filePath);

                // Преобразуем DataTable в список списков динамических объектов
                data = new List<List<dynamic>>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var rowData = new List<dynamic>();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        rowData.Add(row[column]);
                    }
                    data.Add(rowData);
                }

                // Если mappings не заданы, создаем их на основе столбцов DataTable
                if (mappings == null)
                {
                    mappings = new List<string>();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        mappings.Add(column.ColumnName);
                    }
                }
            }
            else
            {
                MessageBox.Show("Неверный режим работы");
                return;
            }

            // Проверяем, что данные получены
            if (data == null || data.Count == 0)
            {
                //MessageBox.Show("Нет данных для отображения");
                return;
            }

            // Очищаем коллекцию Parameters перед наполнением
            Parameters.Clear();

            // Наполняем коллекцию Parameters
            foreach (var attributeValues in data)
            {
                var parameterData = new ParameterData();

                // Заполняем значения атрибутов
                for (int i = 0; i < attributeValues.Count; i++)
                {
                    string attributeName = mappings != null && i < mappings.Count ? mappings[i] : $"Attribute{i}";
                    parameterData.Attributes[attributeName] = attributeValues[i]?.ToString() ?? string.Empty;
                }

                Parameters.Add(parameterData);
            }

            // Устанавливаем источник данных для DataGrid
            dataGrid.ItemsSource = Parameters;

            // Создаем динамические столбцы
            if (mappings != null)
            {
                // Преобразуем список строк в список AttributeMapping
                var attributeMappings = ConvertToAttributeMappings(mappings);
                CreateDynamicColumns(attributeMappings);
            }
        }


        private List<AttributeMapping> ConvertToAttributeMappings(List<string> attributeNames)
        {
            var mappings = new List<AttributeMapping>();
            foreach (var attributeName in attributeNames)
            {
                mappings.Add(new AttributeMapping
                {
                    AttributeName = attributeName,
                    DisplayName = attributeName // Используем имя атрибута как отображаемое имя
                });
            }
            return mappings;
        }

        /// <summary>
        /// Создает динамические столбцы в DataGrid.
        /// </summary>
        private void CreateDynamicColumns(List<AttributeMapping> mappings)
        {
            // Очищаем существующие колонки
            dataGrid.Columns.Clear();

            // Добавляем колонку с чекбоксом
            dataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Выбор",
                Binding = new Binding("IsSelected")
            });

            // Создаем колонки для атрибутов
            foreach (var mapping in mappings)
            {
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = mapping.DisplayName,
                    Binding = new Binding($"Attributes[{mapping.AttributeName}]")
                });
            }
        }

        /// <summary>
        /// Класс для хранения данных параметров.
        /// </summary>
        public class ParameterData
        {
            // Свойство для чекбокса
            public bool IsSelected { get; set; }

            // Словарь для хранения значений атрибутов
            public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Класс для связи отображаемых имен и атрибутов.
        /// </summary>
        public class AttributeMapping
        {
            public string DisplayName { get; set; } // Отображаемое имя в интерфейсе
            public string AttributeName { get; set; } // Имя атрибута в данных
        }

        /// <summary>
        /// Обработчик поиска с поддержкой отмены и выделения всех совпадений
        /// </summary>
        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                bool isFound = DataGridSearchService.FindTextInDataGrid(dataGrid, searchTextBox.Text);

                resultTextBlock.Text = isFound ? "Найдено совпадение!" : "Совпадений нет";
                resultTextBlock.Foreground = isFound ? Brushes.Green : Brushes.Red;
            }
            catch (OperationCanceledException) { /* Поиск отменён */ }

        }



        private void CheckAll_Click(object sender, RoutedEventArgs e) // Отметить все позиции
        {
            SelectedCount.Text = DataGridSearchService.CheckRowsInDataGrid(dataGrid, DataGridSearchService.CheckAction.All).ToString();
        }

        private void CheckNone_Click(object sender, RoutedEventArgs e) // Снять все отметки
        {
            SelectedCount.Text = DataGridSearchService.CheckRowsInDataGrid(dataGrid, DataGridSearchService.CheckAction.None).ToString();
        }

        private void CheckSelected_Click(object sender, RoutedEventArgs e)
        {
            SelectedCount.Text = DataGridSearchService.CheckRowsInDataGrid(dataGrid, DataGridSearchService.CheckAction.Select).ToString();
        }

        private void ExportToCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            string CSVexportDataFileName = GetActiveDocumentPathInfo(PathInfoType.FileNameWithoutExt) + "_ExportData.csv";
            ExportToCSV.ExportToCsv(dataGrid, ExportHeaders.IsChecked.Value, Path.Combine(ActiveDocPath, CSVexportDataFileName));
            //ExportDataGridToFile.ExportToCsv(dataGrid, ExportHeaders.IsChecked.Value, "C:\\tmp\\KIPdata.csv");
        }

        private void ExportToXlsxBtn_Click(object sender, RoutedEventArgs e)
        {
            string XLSXexportDataFileName = GetActiveDocumentPathInfo(PathInfoType.FileNameWithoutExt) + "_ExportData.xlsx";
            ExportToXLSX.ExportToXlsx(dataGrid, ExportHeaders.IsChecked.Value, Path.Combine(ActiveDocPath, XLSXexportDataFileName));
            //ExportDataGridToFile.ExportToXlsx(dataGrid, ExportHeaders.IsChecked.Value, "C:\\tmp\\KIPdata.csv");
        }

        private void ExportHeaders_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void LoadDataToObjBtn_Click(object sender, RoutedEventArgs e)
        {

            HostMgd.EditorInput.Editor ed = GetActiveDocumentEditor();
            IDsCollector selectionHandler = new IDsCollector(GetActiveDocumentEditor(), "Выберите объекты на чертеже для загрузки в них данных");
            McObjectId[] idsObjSelected = selectionHandler.GetSelectedIds();

            DataGridToObjects.ProcessDataGridToObjects(dataGrid, idsObjSelected);
            ed.Command("REGENALL");
        }

        private void AutoLoadDataToObjBtn_Click(object sender, RoutedEventArgs e)
        {

            KipPosProcessor.KipPosTotalAgregator(dataGrid, KipObjectName, AutoLoadData);
            KipPosProcessor.KipPosTotalAgregator(dataGrid, KipObjectName, HighlightOnly);
            HostMgd.EditorInput.Editor ed = GetActiveDocumentEditor();
            ed.Command("REGENALL");
        }

        private void CheckDataBtn_Click(object sender, RoutedEventArgs e)
        {
            KipPosProcessor.KipPosTotalAgregator(dataGrid, KipObjectName, HighlightOnly); // Вызов метода подсветки отсутствующих поз КИП
            KipPosProcessor.CompareAndHighlightAttributes(dataGrid, KipObjectName); // Вызов метода сравнения и подсветки отличающихся данных
        }

        private void ImportFromFile_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                // Создаем диалоговое окно для выбора файла
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Выберите файл данных",
                    Filter = "Excel Files|*.xlsx|CSV Files|*.csv", // Фильтр для CSV и Excel
                    Multiselect = false // Разрешить выбор только одного файла
                };

                // Показываем диалоговое окно
                if (openFileDialog.ShowDialog() == true)
                {
                    // Получаем путь к выбранному файлу
                    string filePath = openFileDialog.FileName;

                    // Вызываем метод загрузки данных
                    //DataLoader.LoadDataToDataGrid(dataGrid, filePath, AttributeMappingsFilePath);

                    PopulateDataGrid2(2, filePath);


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }
    }
}
