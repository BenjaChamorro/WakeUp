using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCombatData", menuName = "WakeUp/Combat/Enemy Combat Data")]
public class EnemyCombatData : ScriptableObject {
    [Header("Identidad")]
    public string enemyId = "enemy_default";
    public string enemyDisplayName = "Enemigo";

    [Header("Visual")]
    public Sprite enemySprite;
    public Sprite defeatSprite;

    [Header("Dialogos")]
    [TextArea(2, 5)] public List<string> introDialogues = new List<string>();
    [TextArea(2, 5)] public List<string> defeatDialogues = new List<string>();

    [Header("Animacion de derrota")]
    public EnemyDefeatAnimationProfile defeatAnimationProfile;

    [Header("Reglas de victoria")]
    [TextArea(4, 10)] public string victoryCode = string.Empty;

    [Header("Dialogo Clippy")]
    [TextArea(2, 5)] public string dialogoClippy = string.Empty;

    [Header("Bloques temporales del combate")]
    public List<CodeBlockData> combatOnlyBlocks = new List<CodeBlockData>();

    [Header("Bloques Pre-Rellenados")]
    [TextArea(4, 12)]
    public string prefilledBlocksCode = string.Empty;

    [Header("Recompensas")]
    public List<CodeBlockData> unlockBlocksOnDefeat = new List<CodeBlockData>();
}
