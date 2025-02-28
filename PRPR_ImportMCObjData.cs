
 using App = HostMgd.ApplicationServices;
 using Db = Teigha.DatabaseServices;
 using Ed = HostMgd.EditorInput;
 using Rtm = Teigha.Runtime;
 //using Proc = System.Diagnostics.Process; // для запуска explorer чтобы открыть папку
using System;
using nanoCAD_PRPR_WPF;
using System.Windows;
using Multicad.DatabaseServices;
using PRPR_METHODS;
using System.Windows.Controls;
using System.IO;


[assembly: Rtm.CommandClass(typeof(Tools.CadCommand))]

namespace Tools
{
    /// <summary> 
    /// Комманды
    /// </summary>
    class CadCommand : Rtm.IExtensionApplication
    {
        public const string PRPR_commandName = "PRPR_ImportData"; // Имя команды nCAD
        public void Initialize()
        {

            App.DocumentCollection dm = App.Application.DocumentManager;
            Ed.Editor ed = dm.MdiActiveDocument.Editor;

            string sCom = $"{PRPR_commandName} - Модуль имопорта данных из внешнего dwg-файла в текущий";
            ed.WriteMessage(sCom);
        }

        public void Terminate()
        {
            // Пусто
        }

        /// <summary>
        /// Основная команда для вызова из командной строки {PRPR_commandName}
        /// </summary>
        [Rtm.CommandMethod(PRPR_commandName)]

        /// <summary>
        /// Это основной метод 
        /// </summary>
        public static void PRPR_ImportMCObjData()
        {
            MainWindow mainWin = new MainWindow();
            mainWin.Topmost = true;
            mainWin.Show();

        }

        /// <summary>
        /// Это один из обработчиков мультикада. 
        /// Для примера он выводит значение из формы в консоль и в сообщение 
        /// а так же возвращает принятое на вход знаяение + дополнительный текст.
        /// </summary>
        /// /// <param name="value">Описание параметра</param>
        /// <returns>возвращаемое значение - это строка с результатом обработки</returns>
        /// 
        public static string PRPR_dataProc(string value)
        {
            Db.Database db = Db.HostApplicationServices.WorkingDatabase;
            App.Document doc = App.Application.DocumentManager.MdiActiveDocument;
            Ed.Editor ed = doc.Editor;

            ed.WriteMessage(value);
            MessageBox.Show($"Сообщение вызвано обработчиком мультикад. Из поля формы WPF получено значение: {value}", "Окно сообщения");

            // иммитация работы обработчика (добавяляет к входному значению текст)
            string retVal = value + " + данные из обработчика";

            return retVal; // Обработчик вернет результат свой работы
        }



        /// <summary>
        /// Вспомогательный класс для работы с активным документом и списком открытых документов в AutoCAD.
        /// </summary>
        public static class ActiveDocumentHelper
        {
            /// <summary>
            /// Тип информации о пути к файлу.
            /// </summary>
            public enum PathInfoType
            {
                /// <summary>
                /// Полный путь с именем файла и расширением.
                /// </summary>
                FullPath,

                /// <summary>
                /// Путь к каталогу, в котором находится файл.
                /// </summary>
                DirectoryPath,

                /// <summary>
                /// Имя файла с расширением.
                /// </summary>
                FileNameWithExt,

                /// <summary>
                /// Имя файла без расширения.
                /// </summary>
                FileNameWithoutExt
            }

            /// <summary>
            /// Возвращает активный документ.
            /// </summary>
            /// <returns>Активный документ или null, если активный документ отсутствует.</returns>
            public static App.Document GetActiveDocument()
            {
                return App.Application.DocumentManager.MdiActiveDocument;
            }

            /// <summary>
            /// Возвращает редактор активного документа.
            /// </summary>
            /// <returns>Редактор активного документа или null, если активный документ отсутствует.</returns>
            public static Ed.Editor GetActiveDocumentEditor()
            {
                App.Document doc = GetActiveDocument();
                return doc?.Editor; // Проверка на null
            }

            /// <summary>
            /// Возвращает информацию о пути к активному документу в зависимости от выбранного типа.
            /// </summary>
            /// <param name="infoType">Тип информации о пути (полный путь, путь к каталогу, имя файла и т.д.).</param>
            /// <returns>Строка с запрошенной информацией о пути или пустая строка, если активный документ отсутствует.</returns>
            public static string GetActiveDocumentPathInfo(PathInfoType infoType)
            {
                App.Document doc = GetActiveDocument();
                if (doc == null) return string.Empty;

                string fullPath = doc.Name;

                return infoType switch
                {
                    PathInfoType.DirectoryPath => Path.GetDirectoryName(fullPath),
                    PathInfoType.FileNameWithExt => Path.GetFileName(fullPath),
                    PathInfoType.FileNameWithoutExt => Path.GetFileNameWithoutExtension(fullPath),
                    PathInfoType.FullPath => fullPath,
                    _ => string.Empty
                };
            }

            /// <summary>
            /// Возвращает список имен всех открытых документов.
            /// </summary>
            /// <returns>Список имен открытых документов.</returns>
            public static List<string> GetDwgDocsList()
            {
                // Получение списка документов
                List<McDocument> dwgDocsList = McDocumentsManager.GetDocuments();

                // Создание списка для имен документов
                List<string> dwgDocsNameList = new List<string>();

                // Итерация по списку документов и добавление имен в dwgDocsNameList
                foreach (McDocument dwgDoc in dwgDocsList)
                {
                    dwgDocsNameList.Add(dwgDoc.Name);
                }

                // Возврат списка имен документов
                return dwgDocsNameList;
            }

            /// <summary>
            /// Активирует открытый документ по его имени.
            /// </summary>
            /// <param name="dwgDocName">Имя документа, который требуется активировать.</param>
            /// <exception cref="ArgumentNullException">Выбрасывается, если имя документа равно null или пустой строке.</exception>
            public static void ActivateDwgDocByName(string dwgDocName)
            {
                if (string.IsNullOrEmpty(dwgDocName))
                {
                    throw new ArgumentNullException(nameof(dwgDocName), "Имя документа не может быть null или пустой строкой.");
                }

                McDocument doc = McDocument.GetDocument(dwgDocName); // Получение документа по его имени
                doc?.Activate(); // Активация документа (с проверкой на null)
            }
        }


    }
}



