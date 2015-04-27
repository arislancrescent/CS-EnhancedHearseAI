using System;
using System.Collections.Generic;

namespace EnhancedHearseAI
{
    public sealed class Settings
    {
        private Settings()
        {
            Tag = "[ARIS] Enhanced Hearse AI";
        }

        private static readonly Settings _Instance = new Settings();
        public static Settings Instance { get { return _Instance; } }

        public readonly string Tag;
    }
}