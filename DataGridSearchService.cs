using System.Collections;
using System.Windows.Controls;
using System.Windows.Data;
using static PRPR_ImportMCObjData.KIPdataWindow;

namespace PRPR_ImportMCObjData
{
    /// <summary>
    /// Сервис для поиска текста в DataGrid с поддержкой многопоточности
    /// </summary>
    public class DataGridSearchService
    {

        public enum CheckAction
        {
            All,    // Выделить все строки
            None,   // Снять выделение со всех строк
            Select  // Выделить только выбранные строки
        }

        /// <summary>
        /// Выполняет поиск текста в DataGrid
        /// </summary>

        /// <summary>
        /// Ищет текст в указанном столбце DataGrid или во всех столбцах, если номер столбца не указан.
        /// </summary>
        /// <param name="targetDataGrid">DataGrid, в котором выполняется поиск.</param>
        /// <param name="searchText">Текст для поиска.</param>
        /// <param name="colNum">Номер столбца для поиска (-1 для поиска во всех столбцах).</param>
        /// <returns>True, если текст найден, иначе False.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Выбрасывается, если номер столбца выходит за пределы допустимого диапазона.</exception>
        public static bool FindTextInDataGrid(DataGrid targetDataGrid, string searchText, int colNum = -1)
        {
            // Проверка на пустой поисковый текст
            if (string.IsNullOrEmpty(searchText))
            {
                targetDataGrid.SelectedItem = null; // Сброс выделения
                return false; // Возврат False, если текст пустой
            }

            var columns = targetDataGrid.Columns; // Получение столбцов DataGrid

            // Проверка корректности номера столбца
            if (colNum != -1 && (colNum < 0 || colNum >= columns.Count))
                throw new ArgumentOutOfRangeException(nameof(colNum)); // Исключение, если номер столбца некорректен

            var itemsSource = targetDataGrid.ItemsSource as IEnumerable; // Получение источника данных DataGrid
            if (itemsSource == null) return false; // Возврат False, если источник данных пуст

            // Поиск по каждому элементу в источнике данных
            foreach (var item in itemsSource)
            {
                // Получение свойств для поиска (всех или одного столбца)
                var properties = (colNum == -1)
                    ? columns.Select(c => ((c as DataGridTextColumn)?.Binding as Binding)?.Path.Path) // Все столбцы
                    : new[] { ((columns[colNum] as DataGridTextColumn)?.Binding as Binding)?.Path.Path }; // Один столбец

                // Поиск по каждому свойству
                foreach (var propName in properties.Where(p => !string.IsNullOrEmpty(p)))
                {
                    var propValue = item.GetType().GetProperty(propName)?.GetValue(item)?.ToString(); // Получение значения свойства
                    if (propValue?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) // Проверка на совпадение
                    {
                        // Обновление UI в основном потоке
                        //Dispatcher.Invoke(() =>
                        //{
                            targetDataGrid.SelectedItem = item; // Выделение найденного элемента
                            targetDataGrid.ScrollIntoView(item); // Прокрутка к найденному элементу
                        //});
                        return true; // Возврат True, если текст найден
                    }
                }
            }

            targetDataGrid.SelectedItem = null; // Сброс выделения, если текст не найден
            return false; // Возврат False, если текст не найден
        }


        /// <summary>
        /// Применяет указанное действие к чекбоксам в DataGrid.
        /// </summary>
        /// <param name="dataGrid">DataGrid, с которым выполняется работа.</param>
        /// <param name="checkAction">Действие: отметить все, снять все или отметить выбранные строки.</param>
        /// <returns>Количество отмеченных чекбоксов после выполнения действия.</returns>
        /// <exception cref="ArgumentNullException">Если dataGrid равен null.</exception>
        /// <exception cref="InvalidOperationException">Если первый столбец не является DataGridCheckBoxColumn.</exception>
        public static int CheckRowsInDataGrid(DataGrid dataGrid, CheckAction checkAction)
        {
            if (dataGrid == null)
                throw new ArgumentNullException(nameof(dataGrid));

            // Проверяем, что первый столбец - CheckBoxColumn
            if (dataGrid.Columns[0] is not DataGridCheckBoxColumn checkBoxColumn)
                throw new InvalidOperationException("Первый столбец должен быть DataGridCheckBoxColumn.");

            // Получаем свойство, к которому привязан CheckBox
            var bindingPath = ((Binding)checkBoxColumn.Binding)?.Path.Path;
            if (string.IsNullOrEmpty(bindingPath))
                throw new InvalidOperationException("CheckBox column must have a valid binding.");

            int checkedCount = 0;

            // Работаем напрямую с данными, а не с визуальными элементами
            foreach (var item in dataGrid.Items)
            {
                bool shouldCheck = checkAction switch
                {
                    CheckAction.All => true,
                    CheckAction.None => false,
                    CheckAction.Select => dataGrid.SelectedItems.Contains(item),
                    _ => throw new ArgumentOutOfRangeException(nameof(checkAction))
                };

                // Обновляем свойство в данных через рефлексию
                var property = item.GetType().GetProperty(bindingPath);
                if (property?.PropertyType == typeof(bool))
                {
                    property.SetValue(item, shouldCheck);
                    if (shouldCheck) checkedCount++;
                }
            }

            // Обновляем DataGrid (если нужно)
            dataGrid.Items.Refresh();
            return checkedCount;
        }



    }
}
