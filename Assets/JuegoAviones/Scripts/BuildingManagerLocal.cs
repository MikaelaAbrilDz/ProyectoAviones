using UnityEngine;
using System.Collections;

public class BuildingManagerLocal : MonoBehaviour
{
    private Animator animator;
    private ParticleSystem particleSystem;

    void Start()
    {
        
        animator = GetComponent<Animator>();
        particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    public void PlayParticles()
    {
        // Lógica directa sin comprobaciones de red
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
        else
        {
            Debug.LogWarning("ParticleSystem no encontrado en el edificio");
        }
    }

    public void Collapse()
    {
        // Lógica directa sin comprobaciones de red
        if (animator != null)
        {
            animator.SetBool("Collapse", true);
        }
        else
        {
            Debug.LogWarning("Animator no encontrado en el edificio");
        }
    }

    // colapsar y reproducir partículas al mismo tiempo
    public void CollapseWithParticles()
    {
        Collapse();
        PlayParticles();
    }

    // Método para resetear el edificio si es necesario
    public void ResetBuilding()
    {
        if (animator != null)
        {
            animator.SetBool("Collapse", false);
        }

        if (particleSystem != null)
        {
            particleSystem.Stop();
            particleSystem.Clear();
        }
    }
}