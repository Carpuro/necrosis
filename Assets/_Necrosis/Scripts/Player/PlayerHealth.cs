using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Salud del jugador (stub de fase 0).
/// Suficiente para que el Cazador haga daño y la muerte exista.
/// En fase 1 esto se reemplaza por salud por zonas + saturación nano.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            Debug.Log("[NECROSIS] Así es como moriste."); // homenaje obligatorio

            // Animación de muerte (si hay modelo rigged); luego reinicia la escena
            var animator = GetComponent<PlayerController>()?.animator;
            if (animator != null) animator.SetTrigger("Die");

            Invoke(nameof(Reload), 4f); // deja ver la animación antes de recargar
        }
    }

    void Reload()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
