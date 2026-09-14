using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // New Input System
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input.x = (Keyboard.current.dKey.isPressed ? 1 : 0)
                    - (Keyboard.current.aKey.isPressed ? 1 : 0);

            input.y = (Keyboard.current.wKey.isPressed ? 1 : 0)
                    - (Keyboard.current.sKey.isPressed ? 1 : 0);
        }

        Vector3 move = new Vector3(input.x, 0f, input.y);

        if (move.magnitude > 1f)
            move.Normalize();

        bool walking = move.magnitude > 0.1f;

        // MOVE
        if (walking)
        {
            controller.Move(move * moveSpeed * Time.deltaTime);

            // Player movement direction ki taraf face kare
            Quaternion targetRotation = Quaternion.LookRotation(move);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // GRAVITY
        if (controller.isGrounded)
        {
            controller.Move(Vector3.down * 2f * Time.deltaTime);
        }
        else
        {
            controller.Move(Vector3.up * gravity * Time.deltaTime);
        }

        // ANIMATION
        if (animator != null)
        {
            animator.SetBool("isWalking", walking);
            animator.SetFloat("speed", walking ? 1f : 0f);
        }
    }
}