using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCombatData", menuName = "WakeUp/Combat/Enemy Combat Data")]
public class EnemyCombatData : ScriptableObject {
    [Header("Identidad")]
    public string enemyId = "enemy_default";
    public string enemyDisplayName = "Enemigo";

    [Header("Visual")]
    public Sprite enemySprite;

    [Header("Dialogos")]
    [TextArea(2, 5)] public List<string> introDialogues = new List<string>();
    [TextArea(2, 5)] public List<string> defeatDialogues = new List<string>();

    [Header("Reglas de victoria (simple)")]
    public List<string> requiredBlockIds = new List<string>();

    [Header("Bloques temporales del combate")]
    public List<CodeBlockData> combatOnlyBlocks = new List<CodeBlockData>();

    [Header("Recompensas")]
    public List<CodeBlockData> unlockBlocksOnDefeat = new List<CodeBlockData>();
}
