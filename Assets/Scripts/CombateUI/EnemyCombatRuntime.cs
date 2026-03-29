using TMPro;
using UnityEngine;

public class EnemyCombatRuntime : MonoBehaviour {
    [Header("Datos del combate")]
    [SerializeField] private EnemyCombatData currentEnemy;

    [Header("Referencias de escena")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private PlayerCodeInventory playerInventory;
    [SerializeField] private CodePaletteBuilder paletteBuilder;

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
            paletteBuilder.RefreshPalette();
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
}
