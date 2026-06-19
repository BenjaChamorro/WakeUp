using System.Collections.Generic;
using UnityEngine;

public class PlayerCodeInventory : MonoBehaviour {
    [Header("Catalogo total de bloques")]
    [SerializeField] private List<CodeBlockData> allBlocks = new List<CodeBlockData>();

    private readonly HashSet<string> unlockedIds = new HashSet<string>();
    private readonly List<CodeBlockData> unlockedBlocks = new List<CodeBlockData>();

    void Awake() {
        EnsureSaveManagerExists();
        LoadUnlockedBlocksFromSave();
    }

    void Start() {
        EnsureSaveManagerExists();
        LoadUnlockedBlocksFromSave();
    }

    private void EnsureSaveManagerExists() {
        if (SaveManager.Instance != null) {
            return;
        }

        GameObject saveManagerObject = new GameObject("SaveManagers");
        saveManagerObject.AddComponent<SaveManager>();
    }

    public void LoadUnlockedBlocksFromSave() {
        unlockedIds.Clear();

        if (SaveManager.Instance != null) {
            IReadOnlyList<string> savedUnlockedIds = SaveManager.Instance.GetUnlockedBlockIds();
            if (savedUnlockedIds != null) {
                for (int i = 0; i < savedUnlockedIds.Count; i++) {
                    string id = savedUnlockedIds[i];
                    if (!string.IsNullOrWhiteSpace(id)) {
                        unlockedIds.Add(id);
                    }
                }
            }
        }

        unlockedIds.Add(SaveManager.DefaultUnlockedBlockId);

        if (SaveManager.Instance != null) {
            SaveManager.Instance.UnlockCodeBlock(SaveManager.DefaultUnlockedBlockId);
        }

        RefreshUnlockedCache();

        CodePaletteBuilder paletteBuilder = FindObjectOfType<CodePaletteBuilder>(true);
        if (paletteBuilder != null) {
            paletteBuilder.RefreshPalette();
        }
    }

    public IReadOnlyList<CodeBlockData> GetUnlockedBlocks() {
        return unlockedBlocks;
    }

    public IReadOnlyList<CodeBlockData> GetAllBlocks() {
        return allBlocks;
    }

    public bool IsUnlocked(string blockId) {
        if (string.IsNullOrWhiteSpace(blockId)) return false;
        return unlockedIds.Contains(blockId);
    }

    public bool UnlockBlock(string blockId) {
        if (string.IsNullOrWhiteSpace(blockId)) return false;

        bool added = unlockedIds.Add(blockId);
        if (added) {
            if (SaveManager.Instance != null) {
                SaveManager.Instance.UnlockCodeBlock(blockId);
            }
            RefreshUnlockedCache();
            CodePaletteBuilder paletteBuilder = FindObjectOfType<CodePaletteBuilder>(true);
            if (paletteBuilder != null) {
                paletteBuilder.RefreshPalette();
            }
        }

        return added;
    }

    public bool UnlockBlock(CodeBlockData block) {
        if (block == null) return false;
        return UnlockBlock(block.blockId);
    }

    private void RefreshUnlockedCache() {
        unlockedBlocks.Clear();

        for (int i = 0; i < allBlocks.Count; i++) {
            CodeBlockData block = allBlocks[i];
            if (block == null || string.IsNullOrWhiteSpace(block.blockId)) continue;

            if (unlockedIds.Contains(block.blockId)) {
                unlockedBlocks.Add(block);
            }
        }
    }
}
