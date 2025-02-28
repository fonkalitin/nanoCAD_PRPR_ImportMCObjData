
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


        public static List<string> GetDwgDocsList()
        {
            //string fileName = "C:\\temp\\cource.dwg";
            // Открытие документа с указанным именем файла
            //App.Document courceDwg = App.Application.DocumentManager.Open(fileName, true);

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
        /// Активатор открытого документа по его имени
        /// </summary>
        public static void activateDwgDocByName(string dwgDocName)
        {
            McDocument doc = McDocument.GetDocument(dwgDocName); // Получение документа по его имени
            doc.Activate(); // Активация документа
        }

        /// <summary>
        /// Возвращает эдитор активного документа
        /// </summary>
        public static Ed.Editor getActiveDocEditor()
        {
        App.Document doc = App.Application.DocumentManager.MdiActiveDocument;
        Ed.Editor ed = doc.Editor;
            return ed;
        }

        /// <summary>
        /// Возвращает активный документ
        /// </summary>
        public static App.Document getActiveDoc()
        {
            App.Document doc = App.Application.DocumentManager.MdiActiveDocument;
            return doc;
        }


        public static string getActiveDocPath()
        {
            App.Document doc = App.Application.DocumentManager.MdiActiveDocument;

            // ---- Имена и пути оригинального dwg-файла (открытого)
            string dwgName = doc.Name; // метод получения полного пути и имени текущего dwg-файла (db.Filename; // Альтернативный метод)
            string dwgFileDirPath = Path.GetDirectoryName(dwgName); // Путь до каталога dwg файла (без имени файла) 
            string dwgFileName = Path.GetFileName(dwgName); // Только имя самого dwg файла с расширением
            string dwgFileNameNoExt = Path.GetFileNameWithoutExtension(dwgFileName); // Только имя самого dwg файла без расширения

            return dwgFileDirPath;
        }

public static class ActiveDocumentHelper
    {
        /// <summary>
        /// Тип информации о пути к файлу
        /// </summary>
        public enum PathInfoType
        {
            FullPath,          // Полный путь с именем файла и расширением
            DirectoryPath,     // Путь к каталогу
            FileNameWithExt,   // Имя файла с расширением
            FileNameWithoutExt // Имя файла без расширения
        }

        /// <summary>
        /// Возвращает активный документ
        /// </summary>
        public static App.Document GetActiveDocument()
        {
            return App.Application.DocumentManager.MdiActiveDocument;
        }

        /// <summary>
        /// Возвращает редактор активного документа
        /// </summary>
        public static Ed.Editor GetActiveDocumentEditor()
        {
            App.Document doc = GetActiveDocument();
            return doc?.Editor; // Проверка на null
        }

        /// <summary>
        /// Возвращает информацию о пути к активному документу в зависимости от выбранного типа
        /// </summary>
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
    }


}
}



