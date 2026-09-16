#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Tycoon.Data;

namespace Tycoon.Editor
{
    public static class TycoonSetupUtility
    {
        [MenuItem("Tools/Tycoon/Create Sample Office Items")]
        public static void CreateSampleOfficeItems()
        {
            string folderPath = "Assets/Data/TycoonItems";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            CreateOrUpdateItem(folderPath, "item_desk", "Executive Desk", "Meja kerja utama kantor", OfficeItemCategory.Desk, new (string name, int cost, string boost)[]
            {
                ("Meja Kayu Standard", 200, "+5% Professional Look"),
                ("Meja Ergonomis Modern", 500, "+15% Efficiency"),
                ("Meja Direktur Glass Top", 1200, "+35% Prestige")
            });

            CreateOrUpdateItem(folderPath, "item_computer", "Office PC & Workstation", "Komputer kerja karyawan", OfficeItemCategory.Computer, new (string name, int cost, string boost)[]
            {
                ("PC Standar Kantor", 300, "+10% Processing Speed"),
                ("Dual Monitor Workstation", 750, "+25% Productivity"),
                ("Supercomputer Rig", 1800, "+60% Work Output")
            });

            CreateOrUpdateItem(folderPath, "item_chair", "Ergonomic Chair", "Kursi kerja nyaman", OfficeItemCategory.Chair, new (string name, int cost, string boost)[]
            {
                ("Kursi Plastik", 100, "+2% Comfort"),
                ("Kursi Busa Putar", 350, "+10% Ergonomics"),
                ("Kursi Gaming Mesh Premium", 900, "+25% Comfort & Health")
            });

            CreateOrUpdateItem(folderPath, "item_coffee", "Coffee Machine", "Mesin kopi area istirahat", OfficeItemCategory.Breakroom, new (string name, int cost, string boost)[]
            {
                ("Tekh & Tekan Manual", 150, "+5% Energy"),
                ("Mesin Espresso Capsule", 450, "+15% Energy Boost"),
                ("Barista Espresso Machine", 1000, "+35% Team Morale")
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Tycoon Setup", $"Sample Office Items successfully created in '{folderPath}'!", "OK");
        }

        private static void CreateOrUpdateItem(string folder, string id, string name, string desc, OfficeItemCategory category, (string name, int cost, string boost)[] levelInfos)
        {
            string assetPath = $"{folder}/{id}.asset";
            OfficeItemSO item = AssetDatabase.LoadAssetAtPath<OfficeItemSO>(assetPath);

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<OfficeItemSO>();
                AssetDatabase.CreateAsset(item, assetPath);
            }

            item.itemId = id;
            item.itemName = name;
            item.itemDescription = desc;
            item.category = category;
            item.levels.Clear();

            for (int i = 0; i < levelInfos.Length; i++)
            {
                var lvlInfo = levelInfos[i];
                OfficeItemLevelData levelData = new OfficeItemLevelData
                {
                    levelIndex = i + 1,
                    levelName = lvlInfo.name,
                    upgradeCost = lvlInfo.cost,
                    boostDescription = lvlInfo.boost,
                    topdownSprite = null
                };
                item.levels.Add(levelData);
            }

            EditorUtility.SetDirty(item);
        }

        [MenuItem("Tools/Tycoon/Create Sample Characters")]
        public static void CreateSampleCharacters()
        {
            string folderPath = "Assets/Data/TycoonCharacters";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            CreateOrUpdateCharacter(folderPath, "worker_dev_1", "Budi Programmer", "Senior Developer", "Suka minum kopi sambil ngoding bug-free.", 2.2f, 100f);
            CreateOrUpdateCharacter(folderPath, "worker_des_1", "Siti UI/UX", "Lead Designer", "Mendesain antarmuka aplikasi super estetik.", 1.8f, 90f);
            CreateOrUpdateCharacter(folderPath, "worker_pm_1", "Andi PM", "Project Manager", "Pengatur jadwal sprint & reminder deadline.", 2.0f, 110f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Tycoon Setup", $"Sample Characters successfully created in '{folderPath}'!", "OK");
        }

        private static void CreateOrUpdateCharacter(string folder, string id, string name, string title, string bio, float speed, float stamina)
        {
            string assetPath = $"{folder}/{id}.asset";
            CharacterSO character = AssetDatabase.LoadAssetAtPath<CharacterSO>(assetPath);

            if (character == null)
            {
                character = ScriptableObject.CreateInstance<CharacterSO>();
                AssetDatabase.CreateAsset(character, assetPath);
            }

            character.characterId = id;
            character.characterName = name;
            character.jobTitle = title;
            character.bio = bio;
            character.baseWalkSpeed = speed;
            character.maxStamina = stamina;

            EditorUtility.SetDirty(character);
        }
    }
}
#endif
