using Multicad;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PRPR_ImportMCObjData
{
    /// <summary>
    /// Логика взаимодействия для KIPdataWindow.xaml
    /// </summary>
    public partial class KIPdataWindow : Window
    {
        // Коллекция для хранения данных параметров
        public ObservableCollection<ParameterData> Parameters { get; set; }

        // Метод получения сопоставлений атрибутов
        public static List<AttributeMapping> GetAttributeMappings()
        {
            return new List<AttributeMapping>
            {
                new AttributeMapping { AttributeName = "IsSelected", DisplayName = "Выбор" },
                new AttributeMapping { AttributeName = "Pos", DisplayName = "Позиция" },
                new AttributeMapping { AttributeName = "Proc_Place_Name", DisplayName = "Место установки" },
                new AttributeMapping { AttributeName = "Media_WTemp", DisplayName = "Температура" },
                new AttributeMapping { AttributeName = "Media_WPress", DisplayName = "Давление" }
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
                    Pos = attributeValues[1].Value.ToString(),
                    Proc_Place_Name = attributeValues[2].Value.ToString(),
                    Media_WTemp = attributeValues[3].Value.ToString(),
                    Media_WPress = attributeValues[4].Value.ToString()
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
            public string Pos { get; set; } // Позиция
            public string Proc_Place_Name { get; set; } // Место установки
            public string Media_WTemp { get; set; } // Температура
            public string Media_WPress { get; set; } // Давление
        }

        public class AttributeMapping
        {
            public string AttributeName { get; set; }
            public string DisplayName { get; set; }
        }

    }
}
