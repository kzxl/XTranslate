using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XTranslate.Native;

namespace XTranslate.Input
{
    public class HotkeyForm
    {
        public delegate void HotkeyEventHandler(ushort id, Keys key, Modifiers modifier);

        public event HotkeyEventHandler HotkeyPress;

        public int HotkeyRepeatLimit { get; set; }

        private Stopwatch repeatLimitTimer;

        public HotkeyForm()
        {
            HotkeyRepeatLimit = 1000;
            repeatLimitTimer = Stopwatch.StartNew();
        }

        public void RegisterHotkey(HotkeyInfo hotkeyInfo)
        {
            if (hotkeyInfo != null && hotkeyInfo.Status != HotkeyStatus.Registered)
            {
                if (!hotkeyInfo.IsValidHotkey)
                {
                    hotkeyInfo.Status = HotkeyStatus.NotConfigured;
                    return;
                }

                if (hotkeyInfo.ID == 0)
                {
                    string uniqueID = Helpers.GetUniqueID();
                    hotkeyInfo.ID = NativeMethods.GlobalAddAtom(uniqueID);

                    if (hotkeyInfo.ID == 0)
                    {                        
                        hotkeyInfo.Status = HotkeyStatus.Failed;
                        return;
                    }
                }

                if (!NativeMethods.RegisterHotKey(Handle, hotkeyInfo.ID, (uint)hotkeyInfo.ModifiersEnum, (uint)hotkeyInfo.KeyCode))
                {
                    NativeMethods.GlobalDeleteAtom(hotkeyInfo.ID);
                    
                    hotkeyInfo.ID = 0;
                    hotkeyInfo.Status = HotkeyStatus.Failed;
                    return;
                }

                hotkeyInfo.Status = HotkeyStatus.Registered;
            }
        }

        public bool UnregisterHotkey(HotkeyInfo hotkeyInfo)
        {
            if (hotkeyInfo != null)
            {
                if (hotkeyInfo.ID > 0)
                {
                    bool result = NativeMethods.UnregisterHotKey(Handle, hotkeyInfo.ID);

                    if (result)
                    {
                        NativeMethods.GlobalDeleteAtom(hotkeyInfo.ID);
                        hotkeyInfo.ID = 0;
                        hotkeyInfo.Status = HotkeyStatus.NotConfigured;
                        return true;
                    }
                }

                hotkeyInfo.Status = HotkeyStatus.Failed;
            }

            return false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)WindowsMessages.HOTKEY && CheckRepeatLimitTime())
            {
                ushort id = (ushort)m.WParam;
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                Modifiers modifier = (Modifiers)((int)m.LParam & 0xFFFF);
                OnKeyPressed(id, key, modifier);
                return;
            }

            base.WndProc(ref m);
        }

        protected void OnKeyPressed(ushort id, Keys key, Modifiers modifier)
        {
            HotkeyPress?.Invoke(id, key, modifier);
        }

        private bool CheckRepeatLimitTime()
        {
            if (HotkeyRepeatLimit > 0)
            {
                if (repeatLimitTimer.ElapsedMilliseconds >= HotkeyRepeatLimit)
                {
                    repeatLimitTimer.Reset();
                    repeatLimitTimer.Start();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
