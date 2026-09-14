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
        public List<string> hiredCharacterIds = new List<string>();
        public List<string> activeCharacterIds = new List<string>();
    }
}
