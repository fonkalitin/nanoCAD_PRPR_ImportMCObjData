using HostMgd.EditorInput;
using Multicad.DatabaseServices;
using Multicad;
using Teigha.DatabaseServices;

namespace PRPR_ImportMCObjData
{
    public class IDsCollector
    {
        private Editor ed;
        private string msgOnSelect;

        public IDsCollector(Editor editor, string msgOnSelect)
        {
            ed = editor;
            this.msgOnSelect = "Выберите объекты на чертеже";
        }
// Метод возвращает набор ID получаемый либо из преселекта выбранного набора,
// либо из набора запрошенного у пользователя после вызова команды
        public McObjectId[] GetSelectedIds() 
        {
            int idNum = 0;
            McObjectId[] idSelected = Array.Empty<McObjectId>();

            PromptSelectionResult pickSet = ed.SelectImplied();
            if (pickSet.Status == PromptStatus.OK)
            {
                SelectionSet selValue = pickSet.Value;
                ObjectId[] DbIds = selValue.GetObjectIds(); // Набор ObjectId объектов из преселекта
                ed.WriteMessage($"\nВ предварительном наборе обнаружено объектов: {DbIds.Length}");

                // Переопределение размера массива
                Array.Resize(ref idSelected, DbIds.Length);

                foreach (ObjectId DbId in DbIds)
                {
                    idSelected[idNum] = McObjectId.FromOldIdPtr(DbId.OldIdPtr); // Преобразование ObjectId >>> McObjectId
                    idNum += 1;
                }

            }
            else // Запрос нового выбора объекта у пользователя
            {
                ed.WriteMessage("\nПредварительно выбранных объектов нет");
                idSelected = McObjectManager.SelectObjects(msgOnSelect); // Запрос выбора объектов у пользователя
            }

            ed.SetImpliedSelection(SelectionSet.FromObjectIds(null)); // Перед возвратом набора айдишников обязательно выполняется СБРОС текущей преселекции набора!
            return idSelected;
        }
    }
}
