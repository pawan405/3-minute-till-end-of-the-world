using UnityEngine;

public class GameMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = music;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = 0.7f;
    }
}