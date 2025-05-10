using UnityEngine;

public class ScrewDriverController : MonoBehaviour
{
    [SerializeField] private GameObject completeScrewdriver;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip insertSound;

    public void PlaySoundAndActivateComplete(Vector3 position, Quaternion rotation)
    {
        if (insertSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(insertSound);
        }

        if (completeScrewdriver != null)
        {
            completeScrewdriver.transform.position = position;
            completeScrewdriver.transform.rotation = rotation;
            completeScrewdriver.SetActive(true);
        }
    }
}