using TMPro;
using UnityEngine;

public static class CodeConsoleTypography {
    private const float DefaultFontSize = 32f;

    public static TMP_FontAsset DefaultFont { get; private set; }

    public static void CaptureDefaultFont(TMP_FontAsset fontAsset) {
        if (fontAsset != null) {
            DefaultFont = fontAsset;
        }
    }

    public static void Apply(TextMeshProUGUI text, float? fontSize = null, TextAlignmentOptions? alignment = null) {
        if (text == null) {
            return;
        }

        if (DefaultFont != null) {
            text.font = DefaultFont;
        }

        if (fontSize.HasValue) {
            text.fontSize = fontSize.Value;
        } else if (text.fontSize <= 0f) {
            text.fontSize = DefaultFontSize;
        }

        if (alignment.HasValue) {
            text.alignment = alignment.Value;
        }
    }
}