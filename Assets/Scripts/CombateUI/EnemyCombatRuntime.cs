using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class EnemyCombatRuntime : MonoBehaviour {
    private const string EnemyIdRuntimeBlockId = "enemy_id";
    private const string EnemyIdFallbackText = "enemy_id";
    private static readonly Regex ComparisonRuleRegex = new Regex("^(.+?)\\s*(==|!=|>=|<=|>|<)\\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex PlaceholderTokenRegex = new Regex("^[A-Za-z_]+\\d+$", RegexOptions.Compiled);
    private static readonly Regex TokenRegex = new Regex("==|<=|>=|!=|[A-Za-z_][A-Za-z0-9_]*|\\d+(?:\\.\\d+)?|[^\\s]", RegexOptions.Compiled);
    private static readonly Regex LineReferenceRegex = new Regex("LINEA(\\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        // Cargar enemigo desde GameManager
        if (GameManager.Instance != null && GameManager.Instance.CurrentEnemyAsset != null)
        {
            currentEnemy = GameManager.Instance.CurrentEnemyAsset as EnemyCombatData;
            // if (currentEnemy != null)
            // {
            //     Debug.Log($"EnemyCombatRuntime: cargando enemigo desde GameManager: {currentEnemy.enemyDisplayName}");
            // }
        }

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

    public void ShowDefeatDialogue() {
        if (currentEnemy == null || dialogueText == null) {
            return;
        }

        if (enemySpriteRenderer != null && currentEnemy.defeatSprite != null) {
            enemySpriteRenderer.sprite = currentEnemy.defeatSprite;
        }

        if (currentEnemy.defeatDialogues != null && currentEnemy.defeatDialogues.Count > 0) {
            for (int i = 0; i < currentEnemy.defeatDialogues.Count; i++) {
                string line = currentEnemy.defeatDialogues[i];
                if (!string.IsNullOrWhiteSpace(line)) {
                    dialogueText.text = line;
                    return;
                }
            }
        }

        dialogueText.text = currentEnemy.enemyDisplayName;
    }

    public void ShowEnemyDefeatedMessage() {
        if (dialogueText == null) {
            return;
        }

        dialogueText.text = "¡Has derrotado al enemigo!";
    }

    public void PlayDefeatAnimation() {
        if (currentEnemy == null || currentEnemy.defeatAnimationProfile == null) {
            return;
        }

        if (enemySpriteRenderer != null && currentEnemy.defeatSprite != null) {
            enemySpriteRenderer.sprite = currentEnemy.defeatSprite;
        }

        Transform targetTransform = enemySpriteRenderer != null ? enemySpriteRenderer.transform : transform;
        currentEnemy.defeatAnimationProfile.PlayAnimation(targetTransform);
    }

    public bool TryEvaluateVictory(string playerCode, out string reason) {
        reason = string.Empty;

        if (currentEnemy == null) {
            reason = "No hay enemigo activo.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(currentEnemy.victoryCode)) {
            reason = "El enemigo no tiene reglas de victoria configuradas.";
            return false;
        }

        bool success = TryEvaluateVictoryCode(currentEnemy.victoryCode, playerCode, out reason);
        if (success)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.MarkCurrentEncounterSucceeded();
            }
            GrantEnemyRewards();
            HandleVictory();
        }
        return success;
    }

    public void HandleVictory()
    {
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        ShowDefeatDialogue();
        yield return new WaitForSeconds(2.5f);

        ShowEnemyDefeatedMessage();
        yield return new WaitForSeconds(2f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitCombatAndReturn();
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

    private bool TryEvaluateVictoryCode(string victoryCode, string playerCode, out string reason) {
        reason = string.Empty;

        List<string> templateLines = SplitCodeLines(victoryCode);
        List<string> playerLines = SplitCodeLines(playerCode);

        List<string> executableTemplateLines = new List<string>();
        List<int> executableLineNumbers = new List<int>();
        List<VictoryComparisonRule> comparisonRules = new List<VictoryComparisonRule>();
        List<List<int>> reorderGroups = new List<List<int>>();

        for (int i = 0; i < templateLines.Count; i++) {
            string line = templateLines[i];
            if (string.IsNullOrWhiteSpace(line)) {
                continue;
            }

            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("#")) {
                string condition = trimmed.Substring(1).Trim();
                if (TryParseReorderCondition(condition, out List<int> group)) {
                    if (group.Count > 1) {
                        reorderGroups.Add(group);
                    }
                    continue;
                }

                if (TryParseComparisonRule(condition, out VictoryComparisonRule rule)) {
                    comparisonRules.Add(rule);
                    continue;
                }

                continue;
            }

            executableTemplateLines.Add(line);
            executableLineNumbers.Add(executableTemplateLines.Count);
        }

        if (executableTemplateLines.Count == 0) {
            reason = "La condición de victoria no contiene líneas ejecutables.";
            return false;
        }

        if (!TryMatchTemplateLines(executableTemplateLines, playerLines, reorderGroups, out Dictionary<string, string> bindings, out reason)) {
            return false;
        }

        for (int i = 0; i < comparisonRules.Count; i++) {
            VictoryComparisonRule rule = comparisonRules[i];
            if (!TryEvaluateComparisonRule(rule, bindings, out string ruleReason)) {
                reason = ruleReason;
                return false;
            }
        }

        reason = "La condición de victoria se cumplió.";
        return true;
    }

    private bool TryMatchTemplateLines(List<string> templateLines, List<string> playerLines, List<List<int>> reorderGroups, out Dictionary<string, string> bindings, out string reason) {
        bindings = new Dictionary<string, string>();
        reason = string.Empty;

        List<LineSegment> segments = BuildSegments(templateLines, reorderGroups, out reason);
        if (segments == null) {
            return false;
        }

        int templateLineCount = 0;
        for (int i = 0; i < segments.Count; i++) {
            templateLineCount += segments[i].LineTexts.Count;
        }

        if (playerLines.Count < templateLineCount) {
            reason = "Faltan líneas en el código del jugador.";
            return false;
        }

        string bestReason = string.Empty;
        for (int startIndex = 0; startIndex <= playerLines.Count - templateLineCount; startIndex++) {
            List<string> playerSlice = playerLines.GetRange(startIndex, templateLineCount);
            if (TryMatchSegmentsToPlayerSlice(segments, playerSlice, bindings, out Dictionary<string, string> matchedBindings, out reason)) {
                bindings = matchedBindings;
                return true;
            }

            if (string.IsNullOrWhiteSpace(bestReason) && !string.IsNullOrWhiteSpace(reason)) {
                bestReason = reason;
            }
        }

        if (!string.IsNullOrWhiteSpace(bestReason)) {
            reason = bestReason;
        } else {
            reason = "El código del jugador no contiene la secuencia necesaria para la condición de victoria.";
        }

        return false;
    }

    private bool TryMatchSegmentsToPlayerSlice(List<LineSegment> segments, List<string> playerLines, Dictionary<string, string> inputBindings, out Dictionary<string, string> outputBindings, out string reason) {
        outputBindings = inputBindings;
        reason = string.Empty;

        int playerIndex = 0;
        for (int i = 0; i < segments.Count; i++) {
            LineSegment segment = segments[i];

            if (segment.LineTexts.Count == 1) {
                if (playerIndex >= playerLines.Count) {
                    reason = "Faltan líneas en el código del jugador.";
                    return false;
                }

                if (!TryMatchLineWithAlternatives(segment.LineTexts[0], playerLines[playerIndex], outputBindings, out outputBindings, out reason)) {
                    return false;
                }

                playerIndex++;
                continue;
            }

            if (playerIndex + segment.LineTexts.Count > playerLines.Count) {
                reason = "El bloque con líneas intercambiables no coincide con la cantidad de líneas del jugador.";
                return false;
            }

            List<string> playerSlice = playerLines.GetRange(playerIndex, segment.LineTexts.Count);
            if (!TryMatchReorderableGroup(segment.LineTexts, playerSlice, outputBindings, out outputBindings, out reason)) {
                return false;
            }

            playerIndex += segment.LineTexts.Count;
        }

        if (playerIndex != playerLines.Count) {
            reason = "Sobran líneas dentro de la zona que coincide con la condición de victoria.";
            return false;
        }

        return true;
    }

    private List<LineSegment> BuildSegments(List<string> templateLines, List<List<int>> reorderGroups, out string reason) {
        reason = string.Empty;

        List<LineSegment> segments = new List<LineSegment>();
        HashSet<int> consumed = new HashSet<int>();

        List<List<int>> normalizedGroups = NormalizeReorderGroups(reorderGroups, templateLines.Count, out reason);
        if (normalizedGroups == null) {
            return null;
        }

        for (int i = 0; i < templateLines.Count; i++) {
            if (consumed.Contains(i + 1)) {
                continue;
            }

            List<int> group = FindGroupStartingAt(normalizedGroups, i + 1);
            if (group != null) {
                List<string> groupLines = new List<string>();
                for (int j = 0; j < group.Count; j++) {
                    int lineNumber = group[j];
                    consumed.Add(lineNumber);
                    groupLines.Add(templateLines[lineNumber - 1]);
                }

                segments.Add(new LineSegment(groupLines));
                i = group[group.Count - 1] - 1;
                continue;
            }

            consumed.Add(i + 1);
            segments.Add(new LineSegment(new List<string> { templateLines[i] }));
        }

        return segments;
    }

    private List<List<int>> NormalizeReorderGroups(List<List<int>> reorderGroups, int lineCount, out string reason) {
        reason = string.Empty;

        List<List<int>> normalized = new List<List<int>>();
        for (int i = 0; i < reorderGroups.Count; i++) {
            List<int> group = reorderGroups[i];
            if (group == null || group.Count < 2) {
                continue;
            }

            group.Sort();
            for (int j = 0; j < group.Count; j++) {
                if (group[j] < 1 || group[j] > lineCount) {
                    reason = "Una condición de intercambio de líneas hace referencia a una línea fuera de rango.";
                    return null;
                }
                if (j > 0 && group[j] == group[j - 1]) {
                    continue;
                }
            }

            bool contiguous = true;
            for (int j = 1; j < group.Count; j++) {
                if (group[j] != group[j - 1] + 1) {
                    contiguous = false;
                    break;
                }
            }

            if (!contiguous) {
                reason = "Las condiciones de intercambio de líneas deben referirse a líneas contiguas.";
                return null;
            }

            normalized.Add(group);
        }

        return normalized;
    }

    private List<int> FindGroupStartingAt(List<List<int>> groups, int lineNumber) {
        for (int i = 0; i < groups.Count; i++) {
            List<int> group = groups[i];
            if (group.Count > 0 && group[0] == lineNumber) {
                return group;
            }
        }

        return null;
    }

    private bool TryMatchReorderableGroup(List<string> templateLines, List<string> playerLines, Dictionary<string, string> inputBindings, out Dictionary<string, string> outputBindings, out string reason) {
        outputBindings = null;
        reason = string.Empty;

        bool[] used = new bool[playerLines.Count];
        return TryMatchReorderableGroupRecursive(templateLines, playerLines, inputBindings, used, 0, out outputBindings, out reason);
    }

    private bool TryMatchReorderableGroupRecursive(List<string> templateLines, List<string> playerLines, Dictionary<string, string> currentBindings, bool[] used, int templateIndex, out Dictionary<string, string> outputBindings, out string reason) {
        outputBindings = null;
        reason = string.Empty;

        if (templateIndex >= templateLines.Count) {
            outputBindings = currentBindings;
            return true;
        }

        for (int playerIndex = 0; playerIndex < playerLines.Count; playerIndex++) {
            if (used[playerIndex]) continue;

            string templateLine = templateLines[templateIndex];
            foreach (string candidateTemplateLine in ExpandLineAlternatives(templateLine)) {
                if (!TryMatchLine(candidateTemplateLine, playerLines[playerIndex], currentBindings, out Dictionary<string, string> nextBindings, out reason)) {
                    continue;
                }

                used[playerIndex] = true;
                if (TryMatchReorderableGroupRecursive(templateLines, playerLines, nextBindings, used, templateIndex + 1, out outputBindings, out reason)) {
                    return true;
                }
                used[playerIndex] = false;
            }
        }

        if (string.IsNullOrWhiteSpace(reason)) {
            reason = "No fue posible acomodar las líneas intercambiables con el código del jugador.";
        }

        return false;
    }

    private bool TryMatchLineWithAlternatives(string templateLine, string playerLine, Dictionary<string, string> inputBindings, out Dictionary<string, string> outputBindings, out string reason) {
        outputBindings = null;
        reason = string.Empty;

        List<string> alternatives = ExpandLineAlternatives(templateLine);
        for (int i = 0; i < alternatives.Count; i++) {
            if (TryMatchLine(alternatives[i], playerLine, inputBindings, out outputBindings, out reason)) {
                return true;
            }
        }

        if (string.IsNullOrWhiteSpace(reason)) {
            reason = "Una línea del código del jugador no coincide con la condición de victoria.";
        }

        return false;
    }

    private bool TryMatchLine(string templateLine, string playerLine, Dictionary<string, string> inputBindings, out Dictionary<string, string> outputBindings, out string reason) {
        outputBindings = null;
        reason = string.Empty;

        if (templateLine == null) {
            reason = "La condición de victoria contiene una línea vacía.";
            return false;
        }

        string normalizedTemplate = templateLine.TrimEnd();
        string normalizedPlayer = playerLine == null ? string.Empty : playerLine.TrimEnd();

        if (GetLeadingSpaces(normalizedTemplate) != GetLeadingSpaces(normalizedPlayer)) {
            reason = "La indentación de una línea no coincide con la condición de victoria.";
            return false;
        }

        List<string> templateTokens = TokenizeLine(normalizedTemplate.TrimStart());
        List<string> playerTokens = TokenizeLine(normalizedPlayer.TrimStart());
        if (templateTokens.Count != playerTokens.Count) {
            reason = "Una línea del código del jugador no tiene la misma estructura que la condición de victoria.";
            return false;
        }

        Dictionary<string, string> bindings = new Dictionary<string, string>(inputBindings);
        for (int i = 0; i < templateTokens.Count; i++) {
            string templateToken = templateTokens[i];
            string playerToken = playerTokens[i];

            if (IsPlaceholderToken(templateToken)) {
                if (bindings.TryGetValue(templateToken, out string boundValue)) {
                    if (!string.Equals(boundValue, playerToken)) {
                        reason = $"El valor de '{templateToken}' no coincide entre la condición y el código del jugador.";
                        return false;
                    }
                } else {
                    bindings[templateToken] = playerToken;
                }

                continue;
            }

            if (!string.Equals(templateToken, playerToken)) {
                reason = "Una línea del código del jugador no coincide con la condición de victoria.";
                return false;
            }
        }

        outputBindings = bindings;
        return true;
    }

    private bool TryParseComparisonRule(string condition, out VictoryComparisonRule rule) {
        rule = default;

        if (string.IsNullOrWhiteSpace(condition)) {
            return false;
        }

        Match match = ComparisonRuleRegex.Match(condition);
        if (!match.Success) {
            return false;
        }

        rule = new VictoryComparisonRule {
            LeftOperand = match.Groups[1].Value.Trim(),
            Operator = match.Groups[2].Value.Trim(),
            RightOperand = match.Groups[3].Value.Trim()
        };
        return true;
    }

    private bool TryParseReorderCondition(string condition, out List<int> lineNumbers) {
        lineNumbers = new List<int>();

        if (string.IsNullOrWhiteSpace(condition)) {
            return false;
        }

        if (!condition.Contains("LINEA")) {
            return false;
        }

        MatchCollection matches = LineReferenceRegex.Matches(condition);
        for (int i = 0; i < matches.Count; i++) {
            if (int.TryParse(matches[i].Groups[1].Value, out int lineNumber)) {
                if (!lineNumbers.Contains(lineNumber)) {
                    lineNumbers.Add(lineNumber);
                }
            }
        }

        return lineNumbers.Count > 1;
    }

    private bool TryEvaluateComparisonRule(VictoryComparisonRule rule, Dictionary<string, string> bindings, out string reason) {
        reason = string.Empty;

        if (!TryResolveOperand(rule.LeftOperand, bindings, out string leftValue) || !TryResolveOperand(rule.RightOperand, bindings, out string rightValue)) {
            reason = $"No se pudo resolver la condición de victoria '{rule.LeftOperand} {rule.Operator} {rule.RightOperand}'.";
            return false;
        }

        if (rule.Operator == "==") {
            if (!string.Equals(leftValue, rightValue)) {
                reason = $"La condición '{rule.LeftOperand} == {rule.RightOperand}' no se cumplió.";
                return false;
            }
            return true;
        }

        if (rule.Operator == "!=") {
            if (string.Equals(leftValue, rightValue)) {
                reason = $"La condición '{rule.LeftOperand} != {rule.RightOperand}' no se cumplió.";
                return false;
            }
            return true;
        }

        if (!TryParseNumericValue(leftValue, out double leftNumeric) || !TryParseNumericValue(rightValue, out double rightNumeric)) {
            reason = $"La condición '{rule.LeftOperand} {rule.Operator} {rule.RightOperand}' requiere valores numéricos.";
            return false;
        }

        bool result = rule.Operator switch {
            ">" => leftNumeric > rightNumeric,
            "<" => leftNumeric < rightNumeric,
            ">=" => leftNumeric >= rightNumeric,
            "<=" => leftNumeric <= rightNumeric,
            _ => false
        };

        if (!result) {
            reason = $"La condición '{rule.LeftOperand} {rule.Operator} {rule.RightOperand}' no se cumplió.";
            return false;
        }

        return true;
    }

    private bool TryResolveOperand(string operand, Dictionary<string, string> bindings, out string value) {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(operand)) {
            return false;
        }

        operand = operand.Trim();
        if (IsPlaceholderToken(operand) && bindings.TryGetValue(operand, out string boundValue)) {
            value = boundValue;
            return true;
        }

        value = operand;
        return true;
    }

    private bool TryParseNumericValue(string value, out double numericValue) {
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out numericValue)
               || double.TryParse(value, out numericValue);
    }

    private List<string> SplitCodeLines(string code) {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(code)) {
            return lines;
        }

        string[] rawLines = code.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < rawLines.Length; i++) {
            string line = rawLines[i].TrimEnd();
            if (!string.IsNullOrWhiteSpace(line)) {
                lines.Add(line);
            }
        }

        return lines;
    }

    private List<string> ExpandLineAlternatives(string line) {
        List<string> result = new List<string>();
        if (line == null) {
            return result;
        }

        int openIndex = FindFirstAlternativeBrace(line);
        if (openIndex < 0) {
            result.Add(line);
            return result;
        }

        int closeIndex = FindMatchingBrace(line, openIndex);
        if (closeIndex < 0) {
            result.Add(line);
            return result;
        }

        string prefix = line.Substring(0, openIndex);
        string suffix = line.Substring(closeIndex + 1);
        string inner = line.Substring(openIndex + 1, closeIndex - openIndex - 1);
        List<string> parts = SplitTopLevelAlternatives(inner);

        for (int i = 0; i < parts.Count; i++) {
            string candidate = prefix + parts[i].Trim() + suffix;
            List<string> expanded = ExpandLineAlternatives(candidate);
            for (int j = 0; j < expanded.Count; j++) {
                result.Add(expanded[j]);
            }
        }

        return result;
    }

    private int FindFirstAlternativeBrace(string line) {
        int depth = 0;
        for (int i = 0; i < line.Length; i++) {
            char c = line[i];
            if (c == '{') {
                if (depth == 0 && line.IndexOf('|', i) >= 0) {
                    return i;
                }
                depth++;
            } else if (c == '}') {
                depth = Mathf.Max(0, depth - 1);
            }
        }

        return -1;
    }

    private int FindMatchingBrace(string line, int openIndex) {
        int depth = 0;
        for (int i = openIndex; i < line.Length; i++) {
            if (line[i] == '{') {
                depth++;
            } else if (line[i] == '}') {
                depth--;
                if (depth == 0) {
                    return i;
                }
            }
        }

        return -1;
    }

    private List<string> SplitTopLevelAlternatives(string inner) {
        List<string> parts = new List<string>();
        if (inner == null) {
            return parts;
        }

        StringBuilder part = new StringBuilder();
        int depth = 0;
        for (int i = 0; i < inner.Length; i++) {
            char c = inner[i];
            if (c == '{') {
                depth++;
                part.Append(c);
                continue;
            }

            if (c == '}') {
                depth = Mathf.Max(0, depth - 1);
                part.Append(c);
                continue;
            }

            if (c == '|' && depth == 0) {
                parts.Add(part.ToString());
                part.Length = 0;
                continue;
            }

            part.Append(c);
        }

        parts.Add(part.ToString());
        return parts;
    }

    private List<string> TokenizeLine(string line) {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(line)) {
            return tokens;
        }

        MatchCollection matches = TokenRegex.Matches(line);
        for (int i = 0; i < matches.Count; i++) {
            tokens.Add(matches[i].Value);
        }

        return tokens;
    }

    private int GetLeadingSpaces(string line) {
        if (string.IsNullOrEmpty(line)) return 0;

        int count = 0;
        for (int i = 0; i < line.Length; i++) {
            if (line[i] == ' ') count++; else break;
        }

        return count;
    }

    private bool IsPlaceholderToken(string token) {
        return !string.IsNullOrWhiteSpace(token) && PlaceholderTokenRegex.IsMatch(token);
    }

    private struct VictoryComparisonRule {
        public string LeftOperand;
        public string Operator;
        public string RightOperand;
    }

    private sealed class LineSegment {
        public List<string> LineTexts { get; }

        public LineSegment(List<string> lineTexts) {
            LineTexts = lineTexts ?? new List<string>();
        }
    }
}
