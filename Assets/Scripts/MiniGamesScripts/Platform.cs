using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[ExecuteAlways]
public class Platform : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(3f, 0.5f);
    [SerializeField] private float borderThickness = 0.1f; // en unidades de mundo

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Vector2 lastSize;
    private float lastBorder;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<BoxCollider2D>();
        // No llamar Apply() aquí: cambia SpriteRenderer.sprite, lo que dispara un
        // SendMessage interno hacia el BoxCollider2D, y Unity no permite SendMessage
        // durante OnValidate. Update() detecta el cambio de tamaño al frame siguiente
        // y aplica el layout ahí, fuera de ese contexto restringido.
    }

    void Update()
    {
        if (size != lastSize || borderThickness != lastBorder)
            Apply();
    }

    // Permite a MiniGameRuntime fijar la disposición (tamaño y borde) en tiempo de ejecución.
    public void SetLayout(Vector2 newSize, float newBorder)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<BoxCollider2D>();
        size = newSize;
        borderThickness = newBorder;
        Apply();
    }

    void Apply()
    {
        size.x = Mathf.Max(size.x, 0.01f);
        size.y = Mathf.Max(size.y, 0.01f);
        borderThickness = Mathf.Max(borderThickness, 0.01f);

        int pixelsPerUnit = 64;
        int w = Mathf.Max(Mathf.RoundToInt(size.x * pixelsPerUnit), 2);
        int h = Mathf.Max(Mathf.RoundToInt(size.y * pixelsPerUnit), 2);

        // Convierte el border de unidades de mundo a píxeles
        int borderPx = Mathf.Max(Mathf.RoundToInt(borderThickness * pixelsPerUnit), 1);
        // Limita para que quede al menos 1 pixel de centro
        borderPx = Mathf.Min(borderPx, w / 2 - 1, h / 2 - 1);
        borderPx = Mathf.Max(borderPx, 1);

        sr.sprite = BuildSprite(w, h, borderPx, pixelsPerUnit);
        sr.drawMode = SpriteDrawMode.Simple;

        col.size = size;
        col.offset = Vector2.zero;

        lastSize = size;
        lastBorder = borderThickness;
    }

    static Sprite BuildSprite(int w, int h, int borderPx, int ppu)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x < borderPx || x >= w - borderPx
                             || y < borderPx || y >= h - borderPx;
                pixels[y * w + x] = isBorder
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(0, 0, 0, 255);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            ppu);
    }
}
