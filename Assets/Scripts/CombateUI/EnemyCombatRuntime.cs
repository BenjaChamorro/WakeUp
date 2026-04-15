using TMPro;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatRuntime : MonoBehaviour {
    private const string EnemyIdRuntimeBlockId = "enemy_id";
    private const string EnemyIdFallbackText = "enemy_id";

    [Header("Datos del combate")]
    [SerializeField] private EnemyCombatData currentEnemy;

    [Header("Referencias de escena")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private PlayerCodeInventory playerInventory;
    [SerializeField] private CodePaletteBuilder paletteBuilder;

    private CodeBlockData enemyIdRuntimeBlock;

    void Awake() {
        AutoAssignReferences();
    }

    void Start() {
        ApplyEnemyData();
    }

    public void ApplyEnemyData() {
        if (currentEnemy == null) {
            Debug.LogWarning("EnemyCombatRuntime: no hay EnemyCombatData asignado.");
            if (dialogueText != null) {
                dialogueText.text = "[DEBUG] Asigna un EnemyCombatData para mostrar dialogo de enemigo.";
            }
            if (paletteBuilder != null) {
                paletteBuilder.SetCombatExclusiveBlocks(null);
            }
            return;
        }

        if (enemySpriteRenderer != null && currentEnemy.enemySprite != null) {
            enemySpriteRenderer.sprite = currentEnemy.enemySprite;
        }

        if (dialogueText != null) {
            if (currentEnemy.introDialogues.Count > 0) {
                dialogueText.text = currentEnemy.introDialogues[0];
            } else {
                dialogueText.text = currentEnemy.enemyDisplayName;
            }
        } else {
            Debug.LogWarning("EnemyCombatRuntime: no se encontro TextoDialogo para mostrar dialogos.");
        }

        if (enemyNameText != null) {
            enemyNameText.text = currentEnemy.enemyDisplayName;
        }

        if (paletteBuilder != null) {
            paletteBuilder.SetCombatExclusiveBlocks(BuildCombatPaletteBlocks());
        }
    }

    public void GrantEnemyRewards() {
        if (currentEnemy == null || playerInventory == null) return;

        bool unlockedSomething = false;
        for (int i = 0; i < currentEnemy.unlockBlocksOnDefeat.Count; i++) {
            CodeBlockData reward = currentEnemy.unlockBlocksOnDefeat[i];
            if (reward == null) continue;

            if (playerInventory.UnlockBlock(reward)) {
                unlockedSomething = true;
            }
        }

        if (unlockedSomething && paletteBuilder != null) {
            paletteBuilder.RefreshPalette();
        }
    }

    private void AutoAssignReferences() {
        if (enemySpriteRenderer == null) {
            enemySpriteRenderer = FindObjectOfType<SpriteRenderer>();
        }

        if (dialogueText == null) {
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI t in texts) {
                string lower = t.name.ToLower();
                if (lower == "textodialogo" || lower == "dialogotexto" || lower == "dialogue" || lower == "dialogotext") {
                    dialogueText = t;
                    break;
                }
            }

            if (dialogueText == null) {
                foreach (TextMeshProUGUI t in texts) {
                    if (t.name.ToLower().Contains("dialog")) {
                        dialogueText = t;
                        break;
                    }
                }
            }

            if (dialogueText == null && texts.Length > 0) {
                dialogueText = texts[0];
            }
        }

        if (enemyNameText == null) {
            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI t in texts) {
                string lower = t.name.ToLower();
                if (lower == "textonombreenemigo" || lower == "enemigoname" || lower == "enemyname") {
                    enemyNameText = t;
                    break;
                }
            }

            if (enemyNameText == null) {
                foreach (TextMeshProUGUI t in texts) {
                    if (t.name.ToLower().Contains("nombre") && t.name.ToLower().Contains("enemigo")) {
                        enemyNameText = t;
                        break;
                    }
                }
            }
        }

        if (playerInventory == null) {
            playerInventory = FindObjectOfType<PlayerCodeInventory>();
        }

        if (paletteBuilder == null) {
            paletteBuilder = FindObjectOfType<CodePaletteBuilder>(true);
        }
    }

    private List<CodeBlockData> BuildCombatPaletteBlocks() {
        List<CodeBlockData> blocks = new List<CodeBlockData>();

        CodeBlockData enemyIdBlock = GetOrCreateEnemyIdRuntimeBlock();
        if (enemyIdBlock != null) {
            blocks.Add(enemyIdBlock);
        }

        if (currentEnemy != null && currentEnemy.combatOnlyBlocks != null) {
            for (int i = 0; i < currentEnemy.combatOnlyBlocks.Count; i++) {
                CodeBlockData block = currentEnemy.combatOnlyBlocks[i];
                if (block == null) continue;
                blocks.Add(block);
            }
        }

        return blocks;
    }

    private CodeBlockData GetOrCreateEnemyIdRuntimeBlock() {
        if (enemyIdRuntimeBlock == null) {
            enemyIdRuntimeBlock = ScriptableObject.CreateInstance<CodeBlockData>();
            enemyIdRuntimeBlock.hideFlags = HideFlags.HideAndDontSave;
            enemyIdRuntimeBlock.blockId = EnemyIdRuntimeBlockId;
            enemyIdRuntimeBlock.commandType = "definition";
        }

        string enemyIdText = EnemyIdFallbackText;
        if (currentEnemy != null && !string.IsNullOrWhiteSpace(currentEnemy.enemyId)) {
            enemyIdText = currentEnemy.enemyId.Trim();
        }

        enemyIdRuntimeBlock.displayText = enemyIdText;
        return enemyIdRuntimeBlock;
    }
}
