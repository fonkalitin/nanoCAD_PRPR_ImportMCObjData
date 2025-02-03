
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
            mainWin.ShowDialog();
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

    }
}



