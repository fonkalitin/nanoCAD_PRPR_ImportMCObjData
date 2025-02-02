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
            //dwgDocsNameList.ItemsSource = Tools.CadCommand.GetDwgDocsList(); //new List<string>();
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

    }
}
