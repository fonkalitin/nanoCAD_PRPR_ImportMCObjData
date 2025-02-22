using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalEnums
{
    public enum KipPosProcessMode
    {
        HighlightOnly,        // Только подсветка
        AutoLoadData,         // Только автозагрузка данных
        AutoLoadAndHighlight  // Автозагрузка данных и подсветка
    }
}
