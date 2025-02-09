using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

class ExportToCSV
{
    public static void ExportToCsv(DataGrid dataGrid, string filePath)
    {
        if (dataGrid.Columns.Count < 2)
            throw new InvalidOperationException("DataGrid должен иметь как минимум два столбца.");

        var checkBoxColumn = dataGrid.Columns[0] as DataGridCheckBoxColumn
            ?? throw new InvalidOperationException("Первый столбец должен быть DataGridCheckBoxColumn.");

        var checkBoxBinding = checkBoxColumn.Binding as Binding
            ?? throw new InvalidOperationException("Чекбокс столбец должен иметь валидную привязку.");

        string checkBoxPropertyPath = checkBoxBinding.Path.Path;
        var culture = new CultureInfo("ru-RU");

        using (var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
        {
            var headers = dataGrid.Columns.Skip(1).Select(c => $"\"{c.Header?.ToString()?.Trim() ?? ""}\"");
            writer.WriteLine(string.Join(";", headers));

            foreach (var item in dataGrid.Items)
            {
                if (!GetCheckBoxValue(item, checkBoxPropertyPath)) continue;

                var cells = dataGrid.Columns.Skip(1).Select(column =>
                {
                    if (column is DataGridBoundColumn bound)
                    {
                        var bindingPath = (bound.Binding as Binding)?.Path.Path;
                        if (bindingPath == null) return "\"\"";

                        var value = BindingEvaluator.GetValue(item, bindingPath);
                        string text;

                        if (value is IFormattable formattable)
                            text = formattable.ToString(null, culture);
                        else
                            text = Convert.ToString(value)?.Trim() ?? "";

                        // Проверка на дробное число и замена точки на запятую
                        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                            text = text.Replace('.', ',');

                        text = text.Replace("\r", "").Replace("\n", "").Replace("\"", "\"\"");
                        return $"\"{text}\"";
                    }
                    return "\"\"";
                });
                writer.WriteLine(string.Join(";", cells));
            }
        }

        var result = MessageBox.Show("Экспорт завершен успешно. Открыть файл CSV сейчас?", "Успех", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result == MessageBoxResult.Yes)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
    }


    private static bool GetCheckBoxValue(object item, string propertyPath)
    {
        var value = BindingEvaluator.GetValue(item, propertyPath);
        return value is bool b && b;
    }

    public static class BindingEvaluator
    {
        public static object GetValue(object source, string propertyPath)
        {
            var binding = new Binding(propertyPath) { Source = source, Mode = BindingMode.OneTime };
            var dummy = new DummyObject();
            BindingOperations.SetBinding(dummy, DummyObject.ValueProperty, binding);
            return dummy.Value;
        }

        private class DummyObject : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(object), typeof(DummyObject));

            public object Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }
    }
}
