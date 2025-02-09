using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

/// <summary>
/// Предоставляет функционал для экспорта данных DataGrid в CSV файл
/// </summary>
class ExportToCSV
{
    /// <summary>
    /// Экспортирует данные из DataGrid в CSV файл с учетом выбранных строк
    /// </summary>
    /// <param name="dataGrid">Источник данных для экспорта</param>
    /// <param name="exportHeaders">Флаг экспорта заголовков</param>
    /// <param name="filePath">Путь для сохранения файла</param>
    /// <exception cref="InvalidOperationException">Выбрасывается при некорректной структуре DataGrid</exception>
    public static void ExportToCsv(DataGrid dataGrid, bool exportHeaders, string filePath)
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
        var culture = new CultureInfo("ru-RU"); // Установка русской локализации

        // Создание CSV файла с кодировкой UTF-8
        using (var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
        {
            // Запись заголовков при необходимости
            if (exportHeaders)
            {
                // Форматирование заголовков с экранированием кавычек
                var headers = dataGrid.Columns.Skip(1).Select(c => $"\"{c.Header?.ToString()?.Trim() ?? ""}\"");
                writer.WriteLine(string.Join(";", headers));
            }

            // Обработка строк данных
            foreach (var item in dataGrid.Items)
            {
                // Пропуск неотмеченных строк
                if (!GetCheckBoxValue(item, checkBoxPropertyPath)) continue;

                // Формирование строки данных
                var cells = dataGrid.Columns.Skip(1).Select(column =>
                {
                    if (column is DataGridBoundColumn bound)
                    {
                        var bindingPath = (bound.Binding as Binding)?.Path.Path;
                        if (bindingPath == null) return "\"\"";

                        // Получение значения через систему привязок
                        var value = BindingEvaluator.GetValue(item, bindingPath);
                        string text;

                        // Форматирование значений
                        if (value is IFormattable formattable)
                            text = formattable.ToString(null, culture); // Форматирование с учетом культуры
                        else
                            text = Convert.ToString(value)?.Trim() ?? ""; // Базовое преобразование

                        // Корректировка десятичных разделителей
                        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                            text = text.Replace('.', ','); // Замена точки на запятую для RU

                        // Экранирование специальных символов
                        text = text.Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\"", "\"\""); // Двойные кавычки для CSV
                        return $"\"{text}\"";
                    }
                    return "\"\""; // Пустое значение по умолчанию
                });
                writer.WriteLine(string.Join(";", cells));
            }
        }

        // Диалог подтверждения успешного экспорта
        var result = MessageBox.Show(
            "Экспорт завершен успешно. Открыть файл CSV сейчас?",
            "Успех",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
        }
    }

    /// <summary>
    /// Получает значение чекбокса из объекта данных
    /// </summary>
    /// <param name="item">Элемент данных</param>
    /// <param name="propertyPath">Путь к свойству</param>
    /// <returns>Состояние чекбокса</returns>
    private static bool GetCheckBoxValue(object item, string propertyPath)
    {
        var value = BindingEvaluator.GetValue(item, propertyPath);
        return value is bool b && b;
    }

    /// <summary>
    /// Вспомогательный класс для работы с привязками данных
    /// </summary>
    public static class BindingEvaluator
    {
        /// <summary>
        /// Получает значение свойства через систему привязок WPF
        /// </summary>
        /// <param name="source">Исходный объект</param>
        /// <param name="propertyPath">Путь к свойству</param>
        /// <returns>Значение свойства</returns>
        public static object GetValue(object source, string propertyPath)
        {
            // Настройка привязки
            var binding = new Binding(propertyPath)
            {
                Source = source,
                Mode = BindingMode.OneTime
            };

            // Использование DependencyObject для получения значения
            var dummy = new DummyObject();
            BindingOperations.SetBinding(dummy, DummyObject.ValueProperty, binding);
            return dummy.Value;
        }

        /// <summary>
        /// Вспомогательный класс для реализации привязки
        /// </summary>
        private class DummyObject : DependencyObject
        {
            /// <summary>
            /// DependencyProperty для хранения значения
            /// </summary>
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register(
                    "Value",
                    typeof(object),
                    typeof(DummyObject));

            /// <summary>
            /// Значение привязки
            /// </summary>
            public object Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }
    }
}