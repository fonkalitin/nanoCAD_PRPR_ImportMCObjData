using DocumentFormat.OpenXml.Bibliography;
using Multicad;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;



namespace PRPR_ImportMCObjData
{
    /// <summary>
    /// Логика взаимодействия для KIPdataWindow.xaml
    /// Это окно агрегации технологических данных по всем КИПиА
    /// </summary>
    public partial class KIPdataWindow : Window
    {
        //private CancellationTokenSource _cancellationTokenSource;

        // Коллекция для хранения данных параметров
        public ObservableCollection<ParameterData> Parameters { get; set; }

        // Метод получения сопоставлений атрибутов
        public static List<AttributeMapping> GetAttributeMappings()
        {
            return new List<AttributeMapping>
            {
                new AttributeMapping { AttributeName = "IsSelected", DisplayName = "Выбор" },
                new AttributeMapping { AttributeName = "pos", DisplayName = "Позиция" },
                new AttributeMapping { AttributeName = "proc_place_name", DisplayName = "Место установки" },
                new AttributeMapping { AttributeName = "media_wtemp", DisplayName = "Температура" },
                new AttributeMapping { AttributeName = "media_wpress", DisplayName = "Давление" }
            };
        }

        
        public KIPdataWindow()
        {
            InitializeComponent();
            Parameters = new ObservableCollection<ParameterData>();
            PopulateDataGrid();
        }

        private void PopulateDataGrid()
        {
            // Используем ваш список атрибутов
            var attributesList = new List<string> { "pos", "proc_place_name", "media_wtemp", "media_wpress" };
            attributesList.Insert(0, "IsSelected"); // Добавляем для чекбокса

            // Получаем данные через метод
            var data = PRPR_METHODS.PRPR_METHODS.CollectParObjData("Первичный КИПиА v0.6", attributesList, 0);

            // Проверяем, что данные получены
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения.");
                return;
            }

            // Наполняем коллекцию ObservableCollection
            foreach (var attributeValues in data)
            {
                var parameterData = new ParameterData
                {
                    IsSelected = false, // По умолчанию
                    pos = attributeValues[1].Value.ToString(),
                    proc_place_name = attributeValues[2].Value.ToString(),
                    media_wtemp = attributeValues[3].Value.ToString(),
                    media_wpress = attributeValues[4].Value.ToString()
                };

                Parameters.Add(parameterData);
            }

            // Проверяем содержимое ObservableCollection
            if (Parameters.Count == 0)
            {
                MessageBox.Show("ObservableCollection пустая.");
                return;
            }

            dataGrid.ItemsSource = Parameters;

            // Создаем динамические столбцы
            CreateDynamicColumns();
        }

        private void CreateDynamicColumns()
        {
            // Получаем сопоставления атрибутов
            var attributeMappings = GetAttributeMappings();

            // Добавление чекбокса в первый столбец
            DataGridCheckBoxColumn checkBoxColumn = new DataGridCheckBoxColumn
            {
                Header = "Выбор",
                Binding = new Binding("IsSelected")
            };
            dataGrid.Columns.Add(checkBoxColumn);

            // Создание остальных столбцов
            foreach (var mapping in attributeMappings.Where(m => m.AttributeName != "IsSelected"))
            {
                DataGridTextColumn textColumn = new DataGridTextColumn
                {
                    Header = mapping.DisplayName,
                    Binding = new Binding(mapping.AttributeName)
                };
                dataGrid.Columns.Add(textColumn);
            }
        }

        public class ParameterData
        {
            public bool IsSelected { get; set; } // Для чекбокса выбора
            public string pos { get; set; } // Позиция
            public string proc_place_name { get; set; } // Место установки
            public string media_wtemp { get; set; } // Температура
            public string media_wpress { get; set; } // Давление
        }

        public class AttributeMapping
        {
            public string AttributeName { get; set; }
            public string DisplayName { get; set; }
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
            ExportToCSV.ExportToCsv(dataGrid, ExportHeaders.IsChecked.Value, "C:\\tmp\\KIPdata.csv");
            //ExportDataGridToFile.ExportToCsv(dataGrid, ExportHeaders.IsChecked.Value, "C:\\tmp\\KIPdata.csv");
        }

        private void ExportToXlsxBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportToXLSX.ExportToXlsx(dataGrid, ExportHeaders.IsChecked.Value, "C:\\tmp\\KIPdata.xlsx");
            //ExportDataGridToFile.ExportToXlsx(dataGrid, ExportHeaders.IsChecked.Value, "C:\\tmp\\KIPdata.csv");
        }

        private void ExportHeaders_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void LoadDataToObjBtn_Click(object sender, RoutedEventArgs e)
        {

            HostMgd.EditorInput.Editor ed = Tools.CadCommand.getActiveDocEditor();
            IDsCollector selectionHandler = new IDsCollector(Tools.CadCommand.getActiveDocEditor(), "Выберите объекты на чертеже для загрузки в них данных");
            McObjectId[] idsObjSelected = selectionHandler.GetSelectedIds();

            DataGridToObjects.ProcessDataGridToObjects(dataGrid, idsObjSelected);

            ed.Command("REGENALL");
        }

        private void AutoLoadDataToObjBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadDataToKipObjects.AutoLoadDataToKipObjects(dataGrid);
            HostMgd.EditorInput.Editor ed = Tools.CadCommand.getActiveDocEditor();
            ed.Command("REGENALL");
        }
    }
}
