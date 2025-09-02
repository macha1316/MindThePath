using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip rotateSound;
    [SerializeField] private AudioClip selectStageSound;
    [SerializeField] private AudioClip gameStartSound;
    [Header("Additional SFX")]
    [SerializeField] private AudioClip undoSound;
    [SerializeField] private AudioClip switchDimensionSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip stageBuildSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private AudioClip[] clearSounds;
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

    public void PlayRotateSound()
    {
        audioSource.PlayOneShot(rotateSound);
    }

    public void SelectStageSound()
    {
        audioSource.PlayOneShot(selectStageSound);
    }

    public void GameStartSound()
    {
        audioSource.PlayOneShot(gameStartSound);
    }

    public void PlayUndoSound()
    {
        if (undoSound != null) audioSource.PlayOneShot(undoSound);
    }

    public void PlaySwitchDimensionSound()
    {
        if (switchDimensionSound != null) audioSource.PlayOneShot(switchDimensionSound);
    }

    public void PlayClickSound()
    {
        if (clickSound != null) audioSource.PlayOneShot(clickSound);
    }

    public void PlayStageBuildSound()
    {
        if (stageBuildSound != null) audioSource.PlayOneShot(stageBuildSound);
    }

    public void PlayMoveSound()
    {
        if (moveSound != null) audioSource.PlayOneShot(moveSound);
    }

    public void PlayTeleportSound()
    {
        if (teleportSound != null) audioSource.PlayOneShot(teleportSound);
    }

    public void PlayClearSounds()
    {
        if (clearSounds[0] != null) audioSource.PlayOneShot(clearSounds[0]);
        if (clearSounds[0] != null) audioSource.PlayOneShot(clearSounds[1]);
    }
}
