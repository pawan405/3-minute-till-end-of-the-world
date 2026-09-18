using UnityEngine;
using ithappy.Creative_Characters_FREE.Controller;
public class GeneratorController : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private GameObject powerPuzzle;

    [Header("Generator Light")]
    [SerializeField] private Renderer generatorLight;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material greenMaterial;

    [Header("Player Control")]
    [SerializeField] private CharacterMover characterMover;
    [SerializeField] private MovePlayerInput movePlayerInput;

    private bool puzzleSolved = false;

    private void Start()
    {
        // Puzzle starts hidden
        if (powerPuzzle != null)
            powerPuzzle.SetActive(false);

        // Generator starts RED
        if (generatorLight != null && redMaterial != null)
        {
            generatorLight.material = redMaterial;
            Debug.Log("Generator light set to RED");
        }
    }

    public void OpenPuzzle()
    {
        if (puzzleSolved)
            return;

        if (powerPuzzle != null)
            powerPuzzle.SetActive(true);

        if (characterMover != null)
            characterMover.enabled = false;

        if (movePlayerInput != null)
            movePlayerInput.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Power Puzzle Opened");
    }

    public void PuzzleCompleted()
    {
        Debug.Log("=== PUZZLE COMPLETED CALLED ===");

        puzzleSolved = true;

        // Close puzzle
        if (powerPuzzle != null)
        {
            Debug.Log("Turning PowerPuzzle OFF");
            powerPuzzle.SetActive(false);
        }
        else
        {
            Debug.LogError("PowerPuzzle reference is NULL!");
        }
        if (characterMover != null)
            characterMover.enabled = true;

        if (movePlayerInput != null)
            movePlayerInput.enabled = true;
        // Change light
        if (generatorLight != null)
        {
            Debug.Log("Generator Light Renderer found");

            if (greenMaterial != null)
            {
                Debug.Log("Changing material to GREEN");
                generatorLight.material = greenMaterial;
            }
            else
            {
                Debug.LogError("Green Material is NULL!");
            }
        }
        else
        {
            Debug.LogError("Generator Light Renderer is NULL!");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("=== GENERATOR POWER RESTORED ===");
    }

    public bool IsPuzzleSolved()
    {
        return puzzleSolved;
    }
}