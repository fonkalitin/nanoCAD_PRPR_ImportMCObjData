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

namespace nanoCAD_PRPR_WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            dwgDocsNameList.ItemsSource = GetDwgDocsFullNameList();
            //treeView.ItemsSource = GetTreeData().Children;
            //treeView.DataContext = GetStubData();
            //this.DataContext = GetTreeData();
            treeView.ItemsSource = GetTreeData();
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
            var root = new TreeNode("Корень - НОМЕР-РД-ТХ");

            var benzolLine = new TreeNode("Линия 346 (Бензол)");

            var dn50 = new TreeNode("DN50")
            {
                Pressure = "0.12 МПа",
                Temperature = "25°C",
                Flow = "30 м³/ч",
                Children =
        {
            new TreeNode("PT46"),
            new TreeNode("TE689")
        }
            };

            var dn80 = new TreeNode("DN80")
            {
                Pressure = "1.2 МПа",
                Temperature = "30°C",
                Flow = "70 м³/ч",
                Children =
        {
            new TreeNode("FT478")
        }
            };

            benzolLine.Children.Add(dn50);
            benzolLine.Children.Add(dn80);

            root.Children.Add(benzolLine);

            var waterTank = new TreeNode("Емкость E-5 (Вода)")
            {
                Pressure = "1.6 МПа",
                Temperature = "15°C",
                Flow = "-"
            };
            waterTank.Children.Add(new TreeNode("LT345A"));
            waterTank.Children.Add(new TreeNode("LT345B"));

            root.Children.Add(waterTank);

            // Устанавливаем ToolTip для каждого узла, формируя строку из значений свойств
            benzolLine.ToolTip = "Давление: 5 бар, Температура: 20°C, Расход: 100 м³/ч";
            dn50.ToolTip = $"Давление: {dn50.Pressure}, Температура: {dn50.Temperature}, Расход: {dn50.Flow}";
            dn80.ToolTip = $"Давление: {dn80.Pressure}, Температура: {dn80.Temperature}, Расход: {dn80.Flow}";
            waterTank.ToolTip = $"Давление: {waterTank.Pressure}, Температура: {waterTank.Temperature}, Расход: {waterTank.Flow}";

            // Устанавливаем ToolTip для дочерних узлов, если это нужно
            foreach (var child in dn50.Children)
            {
                child.ToolTip = $"Давление: {dn50.Pressure}, Температура: {dn50.Temperature}, Расход: {dn50.Flow}";
            }

            foreach (var child in dn80.Children)
            {
                child.ToolTip = $"Давление: {dn80.Pressure}, Температура: {dn80.Temperature}, Расход: {dn80.Flow}";
            }

            foreach (var child in waterTank.Children)
            {
                child.ToolTip = $"Давление: {waterTank.Pressure}, Температура: {waterTank.Temperature}, Расход: {waterTank.Flow}";
            }

            return new List<TreeNode> { root }; // Возвращаем корень как элемент списка
        }





        public class TreeNode
        {
            public string Name { get; set; }
            public string Pressure { get; set; } // Давление
            public string Temperature { get; set; } // Температура
            public string Flow { get; set; } // Расход
            public string ToolTip { get; set; } // ToolTip
            public List<TreeNode> Children { get; set; } = new List<TreeNode>();

            public TreeNode(string name)
            {
                Name = name;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
