using System;
using System.Collections.Generic;

namespace EnhancedHearseAI
{
    public sealed class Settings
    {
        private Settings()
        {
            Tag = "[ARIS] Enhanced Hearse AI";

            ImmediateRange1 = 4000;
            ImmediateRange2 = 20000;
        }

        private static readonly Settings _Instance = new Settings();
        public static Settings Instance { get { return _Instance; } }

        public readonly string Tag;

        public readonly int ImmediateRange1;
        public readonly int ImmediateRange2;
    }
}