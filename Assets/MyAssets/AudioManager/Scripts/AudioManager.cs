using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip dragSound;
    [SerializeField] private AudioClip dropSound;
    private AudioSource audioSource;

    public static AudioManager Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayDragSound()
    {
        if (dragSound != null)
        {
            audioSource.PlayOneShot(dragSound);
        }
    }

    public void PlayDropSound()
    {
        if (dropSound != null)
        {
            audioSource.PlayOneShot(dropSound);
        }
    }
}
