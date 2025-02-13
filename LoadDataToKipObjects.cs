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

namespace PRPR_ImportMCObjData
{
    internal class LoadDataToKipObjects
    {

        public static void AutoLoadDataToKipObjects(DataGrid dataGrid)
        {

            // Получаем все объекты с атрибутом pos
            string parObjCommonName = "КИПиА";
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

                // Получаем значение pos из текущей строки DataGrid (предполагается, что второй столбец — свойство "Pos")
                string gridPos = (row as ParameterData)?.pos; // Доступ к свойству Pos объекта ParameterData

                // Если позиция не указана, пропускаем строку
                if (string.IsNullOrEmpty(gridPos)) continue;

                // Ищем объект с соответствующим значением pos
                foreach (var (objId, objPos) in collectedData)
                {
                    // Если позиция совпадает
                    if (objPos == gridPos)
                    {
                        // Проверяем, нужно ли пропускать подтверждение
                        if (string.IsNullOrEmpty(objPos) || objPos == "INST" || objPos == "POS")
                        {
                            // Добавляем объект в список выбранных без подтверждения
                            selectedObjectsID.Add(objId);
                        }
                        else
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
                            }
                        }

                        break; // Прерываем цикл, так как позиция уже найдена
                    }
                }

                // Загружаем данные в выбранные объекты
                DataGridToObjects.ProcessDataGridToObjects(dataGrid, selectedObjectsID.ToArray());
            }

        }

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


