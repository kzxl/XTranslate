using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTranslate.functions
{
    public class HotkeysConfig
    {
        public List<HotkeySettings> Hotkeys = HotkeyManager.GetDefaultHotkeyList();
    }
}
