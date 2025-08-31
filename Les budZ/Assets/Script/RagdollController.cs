using System.Collections;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D[] ragdollRigidbodies;

    public bool isRagdollActive;

    void Start()
    {
        EnableRagdoll(false);
    }

    public void EnableRagdoll(bool state)
    {
        foreach (Rigidbody2D rb in ragdollRigidbodies)
        {
            rb.bodyType = state ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        }

        animator.enabled = !state;
        isRagdollActive = state;
    }

    public void SetRagdollVelocity(Vector2 velocity)
    {
        foreach (Rigidbody2D rb in ragdollRigidbodies)
        {
            rb.linearVelocity = velocity;
        }
    }

    // ------------------------------------------------------------------------
    // NOUVELLE MÉTHODE : appliquer une force de rotation sur l'axe Z
    // ------------------------------------------------------------------------
    public void SpinOnZ(float torqueAmount, float duration)
    {
        StartCoroutine(SpinOnZCoroutine(torqueAmount, duration));
    }

    private IEnumerator SpinOnZCoroutine(float torqueAmount, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // On applique un torque à chaque RigidBody2D du ragdoll
            foreach (Rigidbody2D rb in ragdollRigidbodies)
            {
                // ForceMode2D.Force = ajout progressif
                // ForceMode2D.Impulse = coup plus instantané
                rb.AddTorque(torqueAmount, ForceMode2D.Force);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}