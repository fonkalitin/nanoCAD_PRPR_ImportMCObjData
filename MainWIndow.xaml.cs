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
using PRPR_ImportMCObjData;

namespace nanoCAD_PRPR_WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public class Parameter
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public ObservableCollection<Parameter> Parameters { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            dwgDocsNameList.ItemsSource = GetDwgDocsFullNameList();

            // Инициализируем ObservableCollection и устанавливаем её как ItemsSource
            Parameters = new ObservableCollection<Parameter>();
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
                string dwgDocName = selectedDoc.FileName;  // Получаем полный путь
                string fullPath = selectedDoc.FullPath;  // Получаем полный путь
                Tools.CadCommand.activateDwgDocByName(fullPath); //  работаем с полным путем файла
                //treeView.ItemsSource = GetTreeData(); 
                InitializeTreeView(dwgDocName); // Обновление содержимого дерева (подгрузка данных из выбранного чертежа)
            }

        }

        private void InitializeTreeView(string rootName)
        {
            treeView.Items.Clear(); // Очищаем дерево
            var root = GetTreeData(rootName); // Получить дерево данных
            var rootItem = CreateTreeViewItem(root); // Создай TreeViewItem из корневого узла
            rootItem.IsExpanded = true; // Разворачиваем корневой узел
            treeView.Items.Add(rootItem); // Добавляем его в TreeView
        }

        // метод для добавления `TreeViewItem` первоначального узла
        private TreeViewItem CreateTreeViewItem(TreeNode node)
        {
            var treeViewItem = new TreeViewItem { 
                Header = node.Name,
                DataContext = node // Устанавливаем DataContext на текущий узел
            };

            foreach (var child in node.Children)
            {
                treeViewItem.Items.Add(CreateTreeViewItem(child)); // Рекурсивно добавляем дочерние узлы
            }
            return treeViewItem;
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


        private TreeNode GetTreeData(string rootName) //List<TreeNode> GetTreeData()
        {
            var root = new TreeNode(rootName);
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
                string deviceId = deviceValues[1].Value.ToString(); // Позиция КИП - уникальный идентификатор

                string media_wtemp = deviceValues[2].Value.ToString(); // темп
                string media_wpress = deviceValues[3].Value.ToString(); // давлен

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
                    InstallationLine = pipeName,
                    Media_wtemp = media_wtemp,
                    Media_wpress = media_wpress
                };
                pipeNode.Children.Add(deviceNode);
            }

            //return new List<TreeNode> { root };
            return root; // Вернуть только корневой узел
        }


        public class TreeNode
        {
            public string Name { get; set; }
            public string InstallationLine { get; set; } // Для труб будет содержать их имя
            public string Media_wtemp { get; set; }
            public string Media_wpress { get; set; }
            public List<TreeNode> Children { get; } = new List<TreeNode>();

            public TreeNode(string name, string installationLine = null)
            {
                Name = name;
                InstallationLine = installationLine;
            }
        }

        // Метод, который будет вызываться при выборе узла
        private void OnTreeNodeSelected(TreeNode selectedNode)
        {
            // Очиститка параметров перед добавлением новых
            Parameters.Clear();

            if (selectedNode == null) return;

            // Для приборов показываем их параметры
            if (selectedNode.Children.Count == 0) // Если узел прибор
            {
                // Добавляем новые параметры
                Parameters.Add(new Parameter { Name = "Температура", Value = selectedNode.Media_wtemp });
                Parameters.Add(new Parameter { Name = "Давление", Value = selectedNode.Media_wpress });
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
            // Получаем выбранный элемент
            var selectedItem = treeView.SelectedItem as TreeViewItem;

            if (selectedItem != null)
            {

                var node = selectedItem.DataContext as TreeNode; // Извлекаем данные из DataContext

                // Вызываем метод для обработки выбора узла
                OnTreeNodeSelected(node);
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void kipDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ShowKipDataWin_Click_1(object sender, RoutedEventArgs e)
        {
            // Созадаем экземпляр класса окна с данными и показываем его
            var KIPdataWindow = new KIPdataWindow();
            KIPdataWindow.Show();
            
        }
    }
}
