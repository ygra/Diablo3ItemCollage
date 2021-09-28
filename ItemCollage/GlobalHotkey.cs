﻿using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ItemCollage
{
    public class GlobalHotkey
    {
        private int modifier;
        private int key;
        private IntPtr hWnd;
        private int id;

        public GlobalHotkey(int modifier, Keys key, Form form)
        {
            this.modifier = modifier;
            this.key = (int)key;
            this.hWnd = form.Handle;
            id = this.GetHashCode();
        }

        public bool Register()
        {
            return RegisterHotKey(hWnd, id, modifier, key);
        }

        public bool Unregister()
        {
            return UnregisterHotKey(hWnd, id);
        }

        public override int GetHashCode()
        {
            return modifier ^ key ^ hWnd.ToInt32();
        }

        public Keys Key
        {
            get
            {
                return (Keys)key;
            }
            set
            {
                Unregister();
                key = (int)value;
                id = this.GetHashCode();
                Register();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}

public static class Constants
{
    //modifiers
    public const int NOMOD = 0x0000;
    public const int ALT = 0x0001;
    public const int CTRL = 0x0002;
    public const int SHIFT = 0x0004;
    public const int WIN = 0x0008;

    //windows message id for hotkey
    public const int WM_HOTKEY_MSG_ID = 0x0312;
}