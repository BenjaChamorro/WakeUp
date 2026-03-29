using System.Collections.Generic;
using UnityEngine;

public class PlayerCodeInventory : MonoBehaviour {
    [Header("Catalogo total de bloques")]
    [SerializeField] private List<CodeBlockData> allBlocks = new List<CodeBlockData>();

    [Header("Desbloqueados extra al iniciar")]
    [SerializeField] private List<string> unlockedAtStart = new List<string>();

    private readonly HashSet<string> unlockedIds = new HashSet<string>();
    private readonly List<CodeBlockData> unlockedBlocks = new List<CodeBlockData>();

    void Awake() {
        InitializeUnlockedBlocks();
    }

    public void InitializeUnlockedBlocks() {
        unlockedIds.Clear();

        for (int i = 0; i < allBlocks.Count; i++) {
            CodeBlockData block = allBlocks[i];
            if (block != null && block.unlockedAtStart && !string.IsNullOrWhiteSpace(block.blockId)) {
                unlockedIds.Add(block.blockId);
            }
        }

        for (int i = 0; i < unlockedAtStart.Count; i++) {
            string id = unlockedAtStart[i];
            if (!string.IsNullOrWhiteSpace(id)) {
                unlockedIds.Add(id);
            }
        }

        RefreshUnlockedCache();
    }

    public IReadOnlyList<CodeBlockData> GetUnlockedBlocks() {
        return unlockedBlocks;
    }

    public bool IsUnlocked(string blockId) {
        if (string.IsNullOrWhiteSpace(blockId)) return false;
        return unlockedIds.Contains(blockId);
    }

    public bool UnlockBlock(string blockId) {
        if (string.IsNullOrWhiteSpace(blockId)) return false;

        bool added = unlockedIds.Add(blockId);
        if (added) {
            RefreshUnlockedCache();
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
