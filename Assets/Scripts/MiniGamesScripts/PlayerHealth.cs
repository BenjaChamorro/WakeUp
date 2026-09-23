using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public Slider healthBar;

    // Lo escucha MiniGameRuntime para dar el minijuego por perdido.
    public UnityEvent onDied;

    private int currentHealth;

    void Start()
    {
        ResetHealth();
    }

    // Vuelve a la vida máxima. MiniGameRuntime la llama cada vez que se reabre el minijuego.
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth;
    }

    public void TakeDamage(int amount)
    {
        // Ya está muerto: evita disparar Die() otra vez con los proyectiles que sigan cayendo.
        if (currentHealth <= 0)
            return;

        currentHealth -= amount;
        healthBar.value = currentHealth;

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Player muerto");
        onDied.Invoke();
    }
}
