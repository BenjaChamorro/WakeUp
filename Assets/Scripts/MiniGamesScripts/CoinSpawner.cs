using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CoinSpawner : MonoBehaviour {
    public GameObject coinPrefab;
    public int coinsToCollect = 5;
    public float minX = -7f;
    public float maxX = 7f;
    public float minY = -2f;
    public float maxY = 3.5f;

    [SerializeField] private TMP_Text coinCounterText;

    public UnityEvent onAllCoinsCollected;

    // Configuración aplicada por MiniGameRuntime. Si nadie configura el spawner
    // (escena suelta), se usan los valores por defecto de arriba.
    private Sprite coinSprite;
    private Vector2 coinScale = Vector2.zero;
    private bool configured;

    private int collected;
    private int totalSpawned;

    void Start() {
        // Espera un frame: da tiempo a que MiniGameRuntime nos configure antes de arrancar
        // con los valores por defecto. Así la escena suelta sigue funcionando sin runtime.
        StartCoroutine(AutoStartIfUnconfigured());
    }

    private IEnumerator AutoStartIfUnconfigured() {
        yield return null;
        if (!configured) {
            SpawnCoins();
        }
    }

    public void Configure(MiniGameData data) {
        if (data == null) {
            return;
        }

        coinSprite = data.coinSprite;
        coinScale = data.coinScale;
        coinsToCollect = Mathf.Max(1, data.coinsToCollect);
        minX = data.coinSpawnMinX;
        maxX = data.coinSpawnMaxX;
        minY = data.coinSpawnMinY;
        maxY = data.coinSpawnMaxY;

        configured = true;
        SpawnCoins();
    }

    public void SpawnCoins() {
        collected = 0;
        totalSpawned = coinsToCollect;
        UpdateDisplay();

        if (coinPrefab == null) {
            return;
        }

        for (int i = 0; i < coinsToCollect; i++) {
            Vector3 spawnPos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
            GameObject instance = Instantiate(coinPrefab, spawnPos, Quaternion.identity);

            Coin coin = instance.GetComponent<Coin>();
            if (coin != null) {
                coin.Configure(this, coinSprite, coinScale);
            }
        }
    }

    // Activa/desactiva por completo el spawner (oculta el contador y no genera monedas).
    // Lo usa MiniGameRuntime para minijuegos que no usan la mecánica de monedas.
    public void SetSpawnerActive(bool active) {
        gameObject.SetActive(active);
    }

    // Lo llama cada Coin al ser recogida por el jugador.
    public void NotifyCoinCollected() {
        collected = Mathf.Min(collected + 1, totalSpawned);
        UpdateDisplay();

        if (collected >= totalSpawned) {
            onAllCoinsCollected.Invoke();
        }
    }

    private void UpdateDisplay() {
        if (coinCounterText == null) {
            return;
        }

        coinCounterText.text = $"{collected}/{totalSpawned}";
    }
}
