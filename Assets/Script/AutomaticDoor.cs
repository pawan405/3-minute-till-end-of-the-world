using UnityEngine;

public class AutomaticDoor : MonoBehaviour
{
    public Animator doorAnimator;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        doorAnimator.SetTrigger("Open");
    }
}