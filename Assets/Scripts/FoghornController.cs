
using UnityEngine;

[DisallowMultipleComponent]
public class FoghornController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip foghornClip;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public bool PlayFoghorn()
    {
        if (audioSource == null)
        {
            Debug.LogWarning(
                "[Foghorn] AudioSource reference is missing!",
                this
            );

            return false;
        }

        if (foghornClip == null)
        {
            Debug.LogWarning(
                "[Foghorn] Foghorn AudioClip is missing!",
                this
            );

            return false;
        }

        audioSource.PlayOneShot(foghornClip);

        if (enableDebugLogs)
        {
            Debug.Log("[Foghorn] Playing foghorn.", this);
        }

        return true;
    }
}
