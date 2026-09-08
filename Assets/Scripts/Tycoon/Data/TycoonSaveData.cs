using System;
using System.Collections.Generic;

namespace Tycoon.Data
{
    [Serializable]
    public class ItemLevelEntry
    {
        public string itemId;
        public int level;
    }

    [Serializable]
    public class TycoonSaveData
    {
        public int coins;
        public List<ItemLevelEntry> itemLevels = new List<ItemLevelEntry>();
    }
}
