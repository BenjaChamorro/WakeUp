using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MiniGameData", menuName = "WakeUp/MiniGame/MiniGame Data")]
public class MiniGameData : ScriptableObject {
    public enum SpawnPattern {
        RandomRain,    // Un proyectil en una posición aleatoria del rango por cada intervalo.
        FixedColumns,  // Una fila de proyectiles en posiciones fijas equiespaciadas por intervalo.
        Waves          // Una oleada de varios proyectiles aleatorios por intervalo.
    }

    public enum MovementDirection {
        Down,        // Cae verticalmente desde arriba (comportamiento clásico).
        RightToLeft, // Aparece en el borde derecho y viaja horizontalmente hacia la izquierda.
        LeftToRight  // Aparece en el borde izquierdo y viaja horizontalmente hacia la derecha.
    }

    [Header("Identidad")]
    public string miniGameId = "minigame_default";
    public string miniGameDisplayName = "Minijuego";

    [Header("Enemigo - Visual")]
    public Sprite enemySprite;
    [Tooltip("Escala (tamaño) del enemigo. Déjalo en (0,0) para conservar la escala del prefab.")]
    public Vector2 enemyScale = Vector2.zero;

    [Header("Proyectil - Visual")]
    public Sprite projectileSprite;
    [Tooltip("Escala (tamaño) del proyectil. El prefab original usa (4,4). Déjalo en (0,0) para conservar la escala del prefab.")]
    public Vector2 projectileScale = new Vector2(4f, 4f);

    [Header("Proyectil - Comportamiento")]
    public float projectileFallSpeed = 5f;
    public float projectileRotationSpeed = 180f;
    public int projectileDamage = 10;

    [Header("Proyectil - Movimiento")]
    [Tooltip("Down: cae desde arriba (comportamiento clásico). RightToLeft/LeftToRight: se mueve horizontalmente en línea recta de un borde de pantalla al otro.")]
    public MovementDirection movementDirection = MovementDirection.Down;
    [Tooltip("Solo si el movimiento es RightToLeft/LeftToRight: rango vertical (Y) en el que pueden aparecer los proyectiles.")]
    public float spawnMinY = -3f;
    public float spawnMaxY = 2f;
    [Tooltip("Solo si el movimiento es RightToLeft/LeftToRight: posición X (en valor absoluto) del borde de pantalla donde aparecen los proyectiles.")]
    public float horizontalSpawnEdgeX = 9f;

    [Header("Spawn")]
    public SpawnPattern spawnPattern = SpawnPattern.RandomRain;
    [Tooltip("Tiempo (s) entre spawns individuales (RandomRain/FixedColumns) o entre oleadas (Waves).")]
    public float spawnInterval = 1.5f;
    [Tooltip("Solo si el movimiento es Down: rango horizontal (X) de aparición en coordenadas de mundo.")]
    public float spawnMinX = -8f;
    public float spawnMaxX = 8f;
    [Tooltip("Solo si el movimiento es Down. Si está activo, los proyectiles caen desde 'spawnHeight'. Si no, usan la altura del ProjectileSpawner.")]
    public bool overrideSpawnHeight = false;
    public float spawnHeight = 3.8f;

    [Header("Spawn - FixedColumns")]
    [Min(1)] public int columnCount = 4;

    [Header("Spawn - Waves")]
    [Min(1)] public int projectilesPerWave = 3;

    [Header("Tiempo de supervivencia")]
    [Tooltip("Si está desactivado, el contador de tiempo no aparece ni corre (útil para minijuegos donde ganar depende solo de otra condición, como recoger monedas).")]
    public bool useSurvivalTimer = true;
    [Tooltip("Tiempo (s) que el jugador debe sobrevivir para ganar el minijuego. Solo aplica si 'Use Survival Timer' está activo.")]
    public float survivalTime = 30f;

    [Header("Moneda - Activación")]
    [Tooltip("Si está activo, el minijuego genera monedas para recoger. Si no, no aparece ninguna moneda.")]
    public bool useCoins = false;

    [Header("Moneda - Visual")]
    public Sprite coinSprite;
    [Tooltip("Escala (tamaño) de la moneda. Déjalo en (0,0) para conservar la escala del prefab.")]
    public Vector2 coinScale = Vector2.zero;

    [Header("Moneda - Spawn")]
    [Tooltip("Cantidad de monedas que hay que recoger antes de que se acabe el tiempo para ganar.")]
    [Min(1)] public int coinsToCollect = 5;
    [Tooltip("Rango horizontal (X) en coordenadas de mundo donde pueden aparecer las monedas.")]
    public float coinSpawnMinX = -7f;
    public float coinSpawnMaxX = 7f;
    [Tooltip("Rango vertical (Y) en coordenadas de mundo donde pueden aparecer las monedas.")]
    public float coinSpawnMinY = -2f;
    public float coinSpawnMaxY = 3.5f;

    [Header("Disposición de plataformas")]
    [Tooltip("Cada entrada genera una plataforma. Deja la lista vacía para conservar las plataformas que ya tenga la escena.")]
    public List<PlatformPlacement> platforms = new List<PlatformPlacement>();
}

[System.Serializable]
public class PlatformPlacement {
    public string platformName = "Plataform";
    [Tooltip("Posición local respecto al contenedor de plataformas.")]
    public Vector2 localPosition = Vector2.zero;
    [Tooltip("Tamaño de la plataforma en unidades de mundo (lo aplica el script Platform).")]
    public Vector2 size = new Vector2(3f, 0.5f);
    public float borderThickness = 0.1f;
    [Tooltip("Escala local extra. Déjalo en (1,1,1) para que solo mande 'size'.")]
    public Vector3 localScale = Vector3.one;
    [Tooltip("Tag del objeto. Usa 'Floor' para que los proyectiles se destruyan al tocarlo.")]
    public string tag = "Platform";
}
