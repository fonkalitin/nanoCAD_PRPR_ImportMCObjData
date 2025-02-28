using Multicad;
using Multicad.Objects;
using Multicad.Symbols.Tables;
using Multicad.Symbols;
using System;
using static PRPR_ImportMCObjData.KIPdataWindow;
using System.Windows.Controls;
using System.Windows.Data;
using Multicad.DatabaseServices;
using DocumentFormat.OpenXml.Office2013.Drawing;
using System.Windows;
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
        public static void KipPosTotalAgregator(DataGrid dataGrid, string parObjCommonName = "КИП", KipPosProcessMode mode = AutoLoadAndHighlight)
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

        internal static Dictionary<string, Dictionary<string, string>> CollectKipAttributes(
        string parObjCommonName,
        List<string> attributesList,
        int collectArea)
        {
            var attributesData = new Dictionary<string, Dictionary<string, string>>();

            ObjectFilter filter = ObjectFilter.Create(false);
            filter.AddType(McParametricObject.TypeID);
            filter.AddType(McUMarker.TypeID);
            filter.AddType(McTable.TypeID);
            filter.AllObjects = true;

            if (collectArea == 0)
                filter.SetCurrentSheet();
            else if (collectArea == 1)
                filter.SetCurrentDocument();

            List<McObjectId> filterIds = filter.GetObjects();

            foreach (McObjectId id in filterIds)
            {
                McObject currObj = id.GetObject();
                Dictionary<string, string> attributes = new Dictionary<string, string>();
                string posValue = null;

                // Проверяем тип объекта и собираем атрибуты
                if (currObj is McParametricObject currParObj && currParObj.CommonName == parObjCommonName)
                {
                    foreach (var attr in attributesList)
                    {
                        var value = currParObj.Public.GetOrCreate(attr)?.Value?.ToString();
                        attributes[attr] = value ?? string.Empty;
                        if (attr == "pos") posValue = value;
                    }
                }
                else if (currObj is McUMarker currUMarker && currUMarker.DbEntity.ObjectProperties["Name"].ToString() == parObjCommonName)
                {
                    foreach (var attr in attributesList)
                    {
                        var value = currUMarker.DbEntity.ObjectProperties[attr]?.ToString();
                        attributes[attr] = value ?? string.Empty;
                        if (attr == "pos") posValue = value;
                    }
                }
                else if (currObj is McTable currTable && currTable.Title == parObjCommonName)
                {
                    foreach (var attr in attributesList)
                    {
                        var value = currTable.DbEntity.ObjectProperties[attr]?.ToString();
                        attributes[attr] = value ?? string.Empty;
                        if (attr == "pos") posValue = value;
                    }
                }

                if (!string.IsNullOrEmpty(posValue))
                {
                    attributesData[posValue] = attributes;
                }
            }

            return attributesData;
        }

        public static void CompareAndHighlightAttributes(DataGrid dataGrid, string parObjCommonName)
        {
            // 1. Получаем список атрибутов из DataGrid (исключая чекбокс )
            var attributesToCompare = dataGrid.Columns
                .OfType<DataGridTextColumn>()
                .Select(col => (col.Binding as Binding)?.Path.Path)
                .Where(path => path != null && path.StartsWith("Attributes["))
                .Select(path => path.Replace("Attributes[", "").Replace("]", ""))
                //.Where(attr => attr != "pos") // Исключаем pos
                .ToList();
            

            // 2. Получаем данные из чертежа
            var collectedData = CollectKipAttributes(parObjCommonName, attributesToCompare, 1);

            // 3. Проходим по всем строкам DataGrid
            foreach (var item in dataGrid.Items)
            {
                if (!(item is ParameterData paramData)) continue;

                // 4. Получаем pos из текущей строки
                if (!paramData.Attributes.TryGetValue("pos", out var gridPos) || string.IsNullOrEmpty(gridPos))
                    continue;

                // 5. Ищем соответствующие данные в collectedData
                if (!collectedData.TryGetValue(gridPos, out var cadAttributes))
                {
                    MarkRowAsMissing(dataGrid, item); // Подсветка всей строки, если pos не найден
                    continue;
                }

                // 6. Сравниваем атрибуты
                foreach (var attribute in attributesToCompare)
                {
                    // Получаем значение из DataGrid
                    paramData.Attributes.TryGetValue(attribute, out var gridValue);

                    // Получаем значение из CAD
                    cadAttributes.TryGetValue(attribute, out var cadValue);

                    // Сравниваем значения
                    if (gridValue != cadValue)
                    {
                        // Подсвечиваем ячейку и добавляем ToolTip с фактическим значением из CAD
                        MarkCell(dataGrid, item, attribute, Brushes.Orange, cadValue);
                    }
                }
            }
        }

        // Метод для подсветки ячейки
        private static void MarkCell(DataGrid dataGrid, object item, string attribute, Brush color, string cadValue)
        {
            var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
            if (row == null) return;

            var column = dataGrid.Columns
                .OfType<DataGridTextColumn>()
                .FirstOrDefault(c => (c.Binding as Binding)?.Path.Path == $"Attributes[{attribute}]");

            if (column?.GetCellContent(row) is TextBlock textBlock)
            {
                textBlock.Foreground = color;
                textBlock.ToolTip = $"Значение отличается от значения в чертеже: {cadValue ?? "Нет данных"}";
            }
        }

        // Метод для подсветки всей строки
        private static void MarkRowAsMissing(DataGrid dataGrid, object item)
        {
            var row = dataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
            if (row != null)
            {
                row.Background = new SolidColorBrush(Color.FromArgb(50, 255, 165, 0)); // Полупрозрачный оранжевый
                row.ToolTip = "Позиция отсутствует в чертеже";
            }
        }


    }
}


