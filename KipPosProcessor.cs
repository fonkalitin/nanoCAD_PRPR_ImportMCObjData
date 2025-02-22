using Multicad;
using Multicad.Objects;
using Multicad.Symbols.Tables;
using Multicad.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PRPR_ImportMCObjData.KIPdataWindow;
using System.Windows.Controls;
using System.Windows.Data;
using Multicad.DatabaseServices;
using DocumentFormat.OpenXml.Office2013.Drawing;
using Teigha.DatabaseServices;
using System.Windows;
using Teigha.LayerManager;
using System.Windows.Media;
using static InternalEnums.KipPosProcessMode;
using InternalEnums;

namespace PRPR_ImportMCObjData
{
    public class KipPosProcessor
    {


        /// <summary>
        /// Метод выполняет автоматическую загрузку значений всех атрибутов в обьекты с позициями pos 
        /// соответствующими таблице dataGrid. Либо выполняет только проверку на наличие позиций в чертеже
        /// </summary>
        public static void AutoLoadDataToKipObjects(DataGrid dataGrid, string parObjCommonName = "КИПиА", bool onlyCheckPos = false)
        {
            // Получаем все объекты с атрибутом pos
            List<string> attributesList = new List<string> { "pos" };
            int collectArea = 1;
            List<(McObjectId, string)> collectedData = CollectKipPosObjIds(parObjCommonName, attributesList, collectArea);

            // Получаем путь к свойству чекбокса из первого столбца
            var checkBoxColumn = dataGrid.Columns[0] as DataGridCheckBoxColumn;
            string checkBoxPropertyPath = (checkBoxColumn.Binding as Binding).Path.Path;

            // Создаем список McObjectId для выбранных объектов
            List<McObjectId> selectedObjectsID = new List<McObjectId>();

            // Проходим по каждой строке в DataGrid (предполагается, что ItemsSource — ObservableCollection<ParameterData>)
            foreach (var row in dataGrid.Items)
            {
                // Проверяем, отмечена ли галочка в строке через биндинг
                if (!DataGridToObjects.GetCheckBoxValue(row, checkBoxPropertyPath)) continue;

                // Получаем значение атрибута "pos" из текущей строки DataGrid
                string gridPos = (row as ParameterData)?.Attributes["pos"];

                // Если позиция не указана, пропускаем строку
                if (string.IsNullOrEmpty(gridPos)) continue;

                // Ищем объект с соответствующим значением pos
                bool isFound = false;
                foreach (var (objId, objPos) in collectedData)
                {
                    // Если позиция совпадает
                    if (objPos == gridPos)
                    {
                        isFound = true;

                        // Если onlyCheckPos == false, выполняем автозагрузку
                        if (!onlyCheckPos)
                        {
                            // Выводим сообщение с вопросом (WPF MessageBox)
                            MessageBoxResult result = MessageBox.Show(
                                $"Данная поз: {objPos} существует в чертеже. Хотите обновить в ней все данные?",
                                "Подтверждение обновления",
                                MessageBoxButton.YesNo, // Кнопки "Да" и "Нет"
                                MessageBoxImage.Question  // Иконка вопроса
                            );

                            // Если пользователь выбрал "Да"
                            if (result == MessageBoxResult.Yes)
                            {
                                // Добавляем объект в список выбранных
                                selectedObjectsID.Add(objId);
                                break; // Прерываем цикл, так как позиция уже найдена
                            }
                        }
                    }
                    // Проверяем, нужно ли пропускать подтверждение
                    else if (!onlyCheckPos && (string.IsNullOrEmpty(objPos) || objPos == "INST" || objPos == "POS"))
                    {
                        // Добавляем объект в список выбранных без подтверждения
                        selectedObjectsID.Add(objId);
                    }
                }

                // Подсветка текста в DataGrid
                var dataGridRow = dataGrid.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow;
                if (dataGridRow == null) continue;

                // Находим ячейку с атрибутом "pos"
                var posColumn = dataGrid.Columns
                    .OfType<DataGridTextColumn>()
                    .FirstOrDefault(col => (col.Binding as Binding)?.Path.Path == "Attributes[pos]");

                if (posColumn != null)
                {
                    var cellContent = posColumn.GetCellContent(dataGridRow);
                    if (cellContent is TextBlock textBlock)
                    {
                        // Устанавливаем цвет текста в зависимости от результата поиска
                        textBlock.Foreground = isFound
                            ? Brushes.Green  // Найден — зеленый
                            : Brushes.Red;   // Не найден — красный
                    }
                }
            }

            // Если onlyCheckPos == false, выполняем автозагрузку данных
            if (!onlyCheckPos && selectedObjectsID.Count > 0)
            {
                DataGridToObjects.ProcessDataGridToObjects(dataGrid, selectedObjectsID.ToArray());
            }
        }

        public static void KipPosTotalAgregator(DataGrid dataGrid, string parObjCommonName = "КИПиА", KipPosProcessMode mode = AutoLoadAndHighlight)
        {
            // Получаем все объекты с атрибутом pos
            List<string> attributesList = new List<string> { "pos" };
            int collectArea = 1;
            List<(McObjectId, string)> collectedData = CollectKipPosObjIds(parObjCommonName, attributesList, collectArea);

            // Получаем путь к свойству чекбокса из первого столбца
            var checkBoxColumn = dataGrid.Columns[0] as DataGridCheckBoxColumn;
            string checkBoxPropertyPath = (checkBoxColumn.Binding as Binding).Path.Path;

            // Создаем список McObjectId для выбранных объектов
            List<McObjectId> selectedObjectsID = new List<McObjectId>();

            // Проходим по каждой строке в DataGrid
            foreach (var row in dataGrid.Items)
            {
                // Проверяем, отмечена ли галочка в строке через биндинг
                if (!DataGridToObjects.GetCheckBoxValue(row, checkBoxPropertyPath)) continue;

                // Получаем значение атрибута "pos" из текущей строки DataGrid
                string gridPos = (row as ParameterData)?.Attributes["pos"];

                // Если позиция не указана, пропускаем строку
                if (string.IsNullOrEmpty(gridPos)) continue;

                // Ищем объект с соответствующим значением pos
                bool isFound = false;
                foreach (var (objId, objPos) in collectedData)
                {
                    if (objPos == gridPos)
                    {
                        isFound = true;

                        // Если режим включает автозагрузку (AutoLoadData или AutoLoadAndHighlight)
                        if (mode == KipPosProcessMode.AutoLoadData || mode == KipPosProcessMode.AutoLoadAndHighlight)
                        {
                            MessageBoxResult result = MessageBox.Show(
                                $"Данная поз: {objPos} существует в чертеже. Хотите обновить в ней все данные?",
                                "Подтверждение обновления",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question
                            );

                            if (result == MessageBoxResult.Yes)
                            {
                                selectedObjectsID.Add(objId);
                                break;
                            }
                        }
                    }
                    // Проверяем, нужно ли пропускать подтверждение
                    else if ((mode == KipPosProcessMode.AutoLoadData || mode == KipPosProcessMode.AutoLoadAndHighlight) &&
                             (string.IsNullOrEmpty(objPos) || objPos == "INST" || objPos == "POS"))
                    {
                        // Добавляем объект в список выбранных без подтверждения
                        selectedObjectsID.Add(objId);
                    }
                }

                // Если режим включает подсветку (HighlightOnly или AutoLoadAndHighlight)
                if (mode == KipPosProcessMode.HighlightOnly || mode == KipPosProcessMode.AutoLoadAndHighlight)
                {
                    // Получаем DataGridRow для текущего элемента
                    var dataGridRow = dataGrid.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow;
                    if (dataGridRow == null) continue;

                    // Находим ячейку с атрибутом "pos"
                    var posColumn = dataGrid.Columns
                        .OfType<DataGridTextColumn>()
                        .FirstOrDefault(col => (col.Binding as Binding)?.Path.Path == "Attributes[pos]");

                    if (posColumn != null)
                    {
                        var cellContent = posColumn.GetCellContent(dataGridRow);
                        if (cellContent is TextBlock textBlock)
                        {
                            // Устанавливаем цвет текста в зависимости от результата поиска
                            textBlock.Foreground = isFound ? Brushes.Green : Brushes.Red;
                        }
                    }
                }
            }

            // Если режим включает автозагрузку (AutoLoadData или AutoLoadAndHighlight)
            if ((mode == KipPosProcessMode.AutoLoadData || mode == KipPosProcessMode.AutoLoadAndHighlight) && selectedObjectsID.Count > 0)
            {
                DataGridToObjects.ProcessDataGridToObjects(dataGrid, selectedObjectsID.ToArray());
            }
        }


        /// <summary>
        /// Метод ищет по имени все обьекты в чертеже (маркеры, параметрику, таблицы) 
        /// и возвращает список их ID и поз. обозначениий
        /// </summary>
        public static List<(McObjectId, string)> CollectKipPosObjIds(string parObjCommonName, List<string> attributesList, int collectArea)
        {
            var attributesObjsExValues = new List<(McObjectId, string)>();

            ObjectFilter filter = ObjectFilter.Create(false);
            filter.AddType(McParametricObject.TypeID); // Добавляем тип McParametricObject
            filter.AddType(McUMarker.TypeID);         // Добавляем тип McUMarker
            filter.AddType(McTable.TypeID);           // Добавляем тип McTable
            filter.AllObjects = true;

            if (collectArea == 0) { filter.SetCurrentSheet(); } else if (collectArea == 1) { filter.SetCurrentDocument(); }
            List<McObjectId> filterIds = filter.GetObjects();

            foreach (McObjectId id in filterIds)
            {
                McObject currObj = id.GetObject();

                // Проверяем тип объекта и обрабатываем его
                if (currObj is McParametricObject currParObj && currParObj.CommonName == parObjCommonName)
                {
                    // Получаем значение атрибута "pos"
                    string posValue = currParObj.Public.GetOrCreate("pos")?.Value?.ToString();
                    if (!string.IsNullOrEmpty(posValue))
                    {
                        attributesObjsExValues.Add((id, posValue));
                    }
                }

                else if (currObj is McUMarker currUMarker && currUMarker.DbEntity.ObjectProperties["Name"].ToString() == parObjCommonName)
                {
                    // Получаем значение атрибута "pos"
                    string posValue = currUMarker.DbEntity.ObjectProperties["pos"]?.ToString();
                    if (!string.IsNullOrEmpty(posValue))
                    {
                        attributesObjsExValues.Add((id, posValue));
                    }
                }

                else if (currObj is McTable currTable && currTable.Title == parObjCommonName)
                {
                    // Получаем значение атрибута "pos"
                    string posValue = currTable.DbEntity.ObjectProperties["pos"].ToString();
                    if (!string.IsNullOrEmpty(posValue))
                    {
                        attributesObjsExValues.Add((id, posValue));
                    }
                }
            }

            return attributesObjsExValues;
        }

    }
}


