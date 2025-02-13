using Multicad.Objects;
using Multicad.Symbols.Tables;
using Multicad.Symbols;
using Multicad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace PRPR_ImportMCObjData
{
    public class DataGridToObjects
    {
        // Основной метод для обработки данных из DataGrid и записи их в выбранные объекты
        public static void ProcessDataGridToObjects(DataGrid dataGrid, McObjectId[] selectedObjectsID)
        {
            // Проверка минимального количества столбцов
            if (dataGrid.Columns.Count < 2)
                throw new InvalidOperationException("DataGrid должен иметь как минимум два столбца.");

            // Валидация первого столбца как чекбокса
            var checkBoxColumn = dataGrid.Columns[0] as DataGridCheckBoxColumn
                ?? throw new InvalidOperationException("Первый столбец должен быть DataGridCheckBoxColumn.");

            // Получение привязки данных для чекбокса
            var checkBoxBinding = checkBoxColumn.Binding as Binding
                ?? throw new InvalidOperationException("Чекбокс столбец должен иметь валидную привязку.");

            string checkBoxPropertyPath = checkBoxBinding.Path.Path;
            
            int i = 0;  

            // Обработка строк данных
            for (int iRow = 0; iRow < dataGrid.Items.Count; iRow++)
            {
              
                var item = dataGrid.Items[iRow];

                // Пропуск неотмеченных строк
                if (!GetCheckBoxValue(item, checkBoxPropertyPath)) continue;
                if (i >= selectedObjectsID.Length) break; // Защита от выхода за пределы (если достигнут максимум то стоп)

                // Получаем выбранный объект для текущей строки
                McObjectId selectedObjID = selectedObjectsID[i];
                i++;

                // Обработка каждого столбца (начиная со второго, так как первый - чекбокс)
                for (int iCol = 1; iCol < dataGrid.Columns.Count; iCol++)
                {
                    var column = dataGrid.Columns[iCol] as DataGridBoundColumn;
                    if (column == null) continue;

                    var bindingPath = (column.Binding as Binding)?.Path.Path;
                    if (bindingPath == null) continue;

                    // Получаем значение из DataGrid
                    var value = ExportToCSV.BindingEvaluator.GetValue(item, bindingPath);
                    string paramValue = value?.ToString() ?? string.Empty;

                    McObject currObj = selectedObjID.GetObject(); // получаем объект по его ИД.

                    // Записываем значение в объект
                    if (currObj is McUMarker currUmarker)
                    {
                        currUmarker.DbEntity.ObjectProperties[bindingPath] = paramValue;
                        //currUmarker.HighLightObjects(true, Color.LightYellow); // Подсветка связанных объектов
                    }
                    else if (currObj is McParametricObject currParObject)
                    {
                        currParObject.DbEntity.ObjectProperties[bindingPath] = paramValue;
                    }
                    else if (currObj is McTable currTableObject)
                    {
                        currTableObject.DbEntity.ObjectProperties[bindingPath] = paramValue;
                    }
                }
            }
        }

        // Вспомогательный метод для получения значения чекбокса
        public static bool GetCheckBoxValue(object item, string propertyPath)
        {
            var propertyInfo = item.GetType().GetProperty(propertyPath);
            return propertyInfo != null && (bool)propertyInfo.GetValue(item);
        }

    }
}
