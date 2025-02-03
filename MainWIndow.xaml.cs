using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

using PRPR_METHODS;
using static Multicad.DatabaseServices.McDbObject;
using System.Collections.ObjectModel;

namespace nanoCAD_PRPR_WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> Parameters { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            dwgDocsNameList.ItemsSource = GetDwgDocsFullNameList();
            //treeView.ItemsSource = GetTreeData().Children;
            //treeView.DataContext = GetStubData();
            //this.DataContext = GetTreeData();
            treeView.ItemsSource = GetTreeData();

            // Инициализируем ObservableCollection и устанавливаем её как ItemsSource
            Parameters = new ObservableCollection<string>();
            kipDataList.ItemsSource = Parameters; // Установите ItemsSource один раз
        }

        /// <summary>
        /// Обработчик события "Нажатие" кнопки
        /// </summary>
        private void myActionButton_Click(object sender, RoutedEventArgs e)
        {
            // Запись нового значения поля. Данные получены с выхода обработчика мультикад.
            // В качестве аргумента функции обработчику передано значение из первого поля формы WPF
            getValBox.Text = Tools.CadCommand.PRPR_dataProc(setValBox.Text);
        }

        private void GetDwgDocsList_Click(object sender, RoutedEventArgs e)
        {
            dwgDocsNameList.ItemsSource = GetDwgDocsFullNameList();
        }

        private void dwgDocsNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             if (dwgDocsNameList.SelectedItem is DwgDoc selectedDoc)
            {
                string fullPath = selectedDoc.FullPath;  // Получаем полный путь
                Tools.CadCommand.activateDwgDocByName(fullPath); //  работаем с полным путем файла
            }

        }

        public class DwgDoc
        {
            public string FullPath { get; set; }

            // Для отображения в ListBox
            public string FileName => Path.GetFileNameWithoutExtension(FullPath);
        }

        public static List<DwgDoc> GetDwgDocsFullNameList()
        {
            // Получаем список полных путей к файлам
            List<string> fullPaths = Tools.CadCommand.GetDwgDocsList(); // метод получения полных имен DWG-файлов (с полным путем)

            // Создаем список объектов DwgDoc
            List<DwgDoc> dwgDocsList = new List<DwgDoc>();

            foreach (var path in fullPaths)
            {
                dwgDocsList.Add(new DwgDoc { FullPath = path });
            }

            return dwgDocsList;
        }


        private List<TreeNode> GetTreeData()
        {
            var root = new TreeNode("НОМЕР-РД.ТХ");
            var pipeDict = new Dictionary<string, TreeNode>(); // Кэш для труб

            // 1. Собираем только приборы
            var devices = PRPR_METHODS.PRPR_METHODS.CollectParObjData(
            parObjCommonName: "Первичный КИПиА v0.6",
            attributesList: new List<string> { "kip_location", "pos", "media_wtemp", "media_wpress", "proc_place_name" },
            collectArea: 0
            );

            // 2. Строим дерево на основе данных приборов
            foreach (var deviceValues in devices)
            {
                if (deviceValues.Count < 4) continue;

                // Извлекаем данные прибора
                string pipeName = deviceValues[0].Value.ToString(); // placeName = имя трубы
                string deviceId = deviceValues[1].Value.ToString(); // уникальный идентификатор
                string proc_place_name = deviceValues[4].Value.ToString(); // Место в процессе

                // 3. Создаем или находим узел трубы
                if (!pipeDict.TryGetValue(pipeName, out var pipeNode))
                {
                    pipeNode = new TreeNode($"{proc_place_name}: {pipeName}", pipeName);
                    pipeDict.Add(pipeName, pipeNode);
                    root.Children.Add(pipeNode);
                }

                // 4. Добавляем прибор к соответствующей трубе
                var deviceNode = new TreeNode($"+ {deviceId}", pipeName)
                {
                    InstallationLine = pipeName
                };
                pipeNode.Children.Add(deviceNode);
            }

            return new List<TreeNode> { root };
        }


        public class TreeNode
        {
            public string Name { get; set; }
            public string InstallationLine { get; set; } // Для труб будет содержать их имя
            public List<TreeNode> Children { get; } = new List<TreeNode>();

            public TreeNode(string name, string installationLine = null)
            {
                Name = name;
                InstallationLine = installationLine;
            }
        }



        private void OnTreeNodeSelected1(TreeNode selectedNode)
        {
            kipDataList.Items.Clear();

            if (selectedNode == null) return;

            // Для приборов показываем их параметры
            if (selectedNode.Children.Count == 0) // Если узел прибор
            {
                var parameters = new List<string>
        {
            $"Давление: {GetDeviceParam(selectedNode.Name, "media_wpress")}",
            $"Температура: {GetDeviceParam(selectedNode.Name, "media_wpress")}" //,
            //$"Расход: {GetDeviceParam(selectedNode.Name, "расход")}"
        };
                kipDataList.ItemsSource = parameters;
            }
        }

        // Метод, который будет вызываться при выборе узла
        private void OnTreeNodeSelected(TreeNode selectedNode)
        {
            // Очистите параметры перед добавлением новых
            Parameters.Clear();

            if (selectedNode == null) return;

            // Для приборов показываем их параметры
            if (selectedNode.Children.Count == 0) // Если узел прибор
            {
                var pressure = GetDeviceParam(selectedNode.Name, "media_wpress");
                var temperature = GetDeviceParam(selectedNode.Name, "media_temp"); // исправлено: используйте корректный параметр

                // Добавляем новые параметры
                Parameters.Add($"Давление: {pressure}");
                Parameters.Add($"Температура: {temperature}");
                // Также можете добавить дополнительные параметры по мере необходимости
            }
        }


        private string GetDeviceParam(string deviceName, string paramName)
        {
            // Реализация поиска параметра по имени прибора
            var device = PRPR_METHODS.PRPR_METHODS.CollectParObjData(
                "Первичный КИПиА v0.6",
                new List<string> { paramName },
                0
            ).FirstOrDefault(d => d[0].Value.ToString() == deviceName);

            return device?[1].Value.ToString() ?? "N/A";
        }


        private void treeView_SelectedItemChanged(object sender,
                                            RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedNode = e.NewValue as TreeNode;
            OnTreeNodeSelected(selectedNode);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void kipDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
