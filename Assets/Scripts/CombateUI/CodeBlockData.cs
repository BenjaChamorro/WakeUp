using UnityEngine;

[CreateAssetMenu(fileName = "CodeBlockData", menuName = "WakeUp/Combat/Code Block Data")]
public class CodeBlockData : ScriptableObject {
    [Header("Identidad")]
    public string blockId = "print";
    public string displayText = "print()";
    public string commandType = "function";

    [Header("Progreso")]
    public bool unlockedAtStart;
}
