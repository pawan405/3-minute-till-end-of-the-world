using System.Collections;
using TMPro;
using UnityEngine;
using ithappy.Creative_Characters_FREE.Controller;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class TorchInteraction : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const string HandBonePath = "Skeleton/Root/Hips/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand";
    private const float GrabAttachDelay = 0.65f;

    [Header("Torch")]
    [SerializeField] private TorchController torchController;
    [SerializeField] private Light torchLight;

    [Header("Interaction UI")]
    [SerializeField] private TMP_Text interactionPrompt;
    [SerializeField] private GameObject interactionPromptBackdrop;
    [SerializeField] private string interactionKeyLabel = "E";

    [Header("Hand Pickup Pose")]
    [SerializeField] private Vector3 handLocalPosition = new Vector3(0.08f, -0.02f, 0.12f);
    [SerializeField] private Vector3 handLocalRotation = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 handLocalScale = Vector3.one;

    private Collider interactionCollider;
    private CharacterMover nearbyCharacter;
    private CharacterMover holdingCharacter;
    private bool playerInRange;
    private bool isGrabbed;
    private bool isGrabAnimating;
    private bool isTorchOn;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider>();
        interactionCollider.isTrigger = true;

        if (torchController == null)
        {
            torchController = GetComponent<TorchController>();
        }

        if (torchLight == null && torchController != null)
        {
            torchLight = torchController.torchLight;
        }

        if (handLocalScale == Vector3.zero)
        {
            handLocalScale = transform.localScale;
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!playerInRange || isGrabAnimating || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (!isGrabbed)
        {
            BeginGrab();
            return;
        }

        if (!isTorchOn)
        {
            TurnOnTorch();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerTag))
        {
            return;
        }

        nearbyCharacter = other.GetComponentInParent<CharacterMover>();
        playerInRange = nearbyCharacter != null;
        if (playerInRange && !isGrabbed && !isGrabAnimating)
        {
            ShowGrabPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(PlayerTag))
        {
            return;
        }

        if (other.GetComponentInParent<CharacterMover>() != nearbyCharacter)
        {
            return;
        }

        playerInRange = false;
        nearbyCharacter = null;
        if (!isGrabbed && !isGrabAnimating)
        {
            SetPromptVisible(false);
        }
    }

    private void BeginGrab()
    {
        if (nearbyCharacter == null || isGrabAnimating)
        {
            return;
        }

        isGrabAnimating = true;
        SetPromptVisible(false);
        nearbyCharacter.StartGrab();
        StartCoroutine(AttachAfterGrabAnimation(nearbyCharacter));
    }

    private IEnumerator AttachAfterGrabAnimation(CharacterMover character)
    {
        yield return new WaitForSecondsRealtime(GrabAttachDelay);

        if (character == null)
        {
            isGrabAnimating = false;
            yield break;
        }
        Transform hand = character.transform.Find(HandBonePath);


        if (hand == null)
        {
            Debug.LogWarning("Torch grab hand bone was not found: " + HandBonePath, this);
            isGrabAnimating = false;
            if (playerInRange)
            {
                ShowGrabPrompt();
            }
            yield break;
        }

        holdingCharacter = character;
        transform.SetParent(hand, false);
        transform.localPosition = handLocalPosition;
        transform.localRotation = Quaternion.Euler(handLocalRotation);
        transform.localScale = handLocalScale;
        character.FinishGrab();

        interactionCollider.enabled = false;
        isGrabbed = true;
        isGrabAnimating = false;
        SetPromptVisible(true);
        SetPromptText("PRESS [ " + interactionKeyLabel + " ] TO TURN ON TORCH");
    }

    private void TurnOnTorch()
    {
        if (torchController == null)
        {
            return;
        }

        torchController.ToggleTorch();
        isTorchOn = true;
        SetPromptVisible(false);
    }

    private void ShowGrabPrompt()
    {
        SetPromptVisible(true);
        SetPromptText("PRESS [ " + interactionKeyLabel + " ] TO GRAB TORCH");
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPromptBackdrop != null)
        {
            interactionPromptBackdrop.SetActive(visible);
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(visible);
        }
    }

    private void SetPromptText(string text)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.text = text;
        }
    }
}
