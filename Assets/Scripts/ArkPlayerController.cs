using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ArkPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float sprintMultiplier = 1.55f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float mouseSensitivity = 6.2f;
    [SerializeField] private float interactRange = 3.2f;
    [SerializeField] private Transform cameraRoot;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch;
    private IArkInteractable currentInteractable;
    private ArkBuildPlanner buildPlanner;
    private Vector3 lastSafePosition;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        buildPlanner = GetComponent<ArkBuildPlanner>();
        if (cameraRoot == null && Camera.main != null)
        {
            cameraRoot = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lastSafePosition = transform.position;
    }

    private void Update()
    {
        if (ArkGameManager.Instance != null && ArkGameManager.Instance.MenuOpen)
        {
            return;
        }

        if (ArkGameManager.Instance != null && ArkGameManager.Instance.GameOver)
        {
            return;
        }

        Look();
        Move();
        UpdateInteractionTarget();

        bool isPlanningBuild = buildPlanner != null && buildPlanner.IsPlanning;

        if (!isPlanningBuild && WasInteractPressed() && currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }

        if (!isPlanningBuild && WasSecondaryPressed() && currentInteractable is IArkSecondaryInteractable secondaryInteractable)
        {
            secondaryInteractable.SecondaryInteract(this);
        }

        if (!isPlanningBuild && WasPrimaryPressed())
        {
            TryUseAxe();
        }
    }

    private void Look()
    {
        Vector2 lookInput = ReadLookInput();
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        if (cameraRoot == null)
        {
            return;
        }

        pitch = Mathf.Clamp(pitch - lookInput.y * mouseSensitivity, -80f, 80f);
        cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector3 input = ReadMoveInput();
        input = Vector3.ClampMagnitude(input, 1f);

        float speed = IsSprinting() ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 velocity = transform.TransformDirection(input) * speed;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
        KeepInsideWorld();
    }

    private void KeepInsideWorld()
    {
        Vector3 position = transform.position;
        if (ArkWorldRules.IsOutsideWorld(position))
        {
            TeleportTo(ArkWorldRules.ClampToWorld(position));
            return;
        }

        if (controller.isGrounded && position.y > ArkWorldRules.WaterHeight - 0.25f)
        {
            lastSafePosition = position;
        }

        if (position.y < ArkWorldRules.FallRespawnY)
        {
            TeleportTo(lastSafePosition);
        }
    }

    public void TeleportTo(Vector3 position)
    {
        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = position;
        verticalVelocity = -2f;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private void UpdateInteractionTarget()
    {
        currentInteractable = null;
        if (ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.ClearPrompt();
        }

        if (cameraRoot == null)
        {
            return;
        }

        if (!Physics.Raycast(cameraRoot.position, cameraRoot.forward, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            return;
        }

        currentInteractable = hit.collider.GetComponentInParent<IArkInteractable>();
        if (currentInteractable != null && ArkGameManager.Instance != null)
        {
            ArkGameManager.Instance.ShowPrompt(currentInteractable.Prompt);
        }
    }

    private void TryUseAxe()
    {
        if (cameraRoot == null)
        {
            return;
        }

        if (!Physics.Raycast(cameraRoot.position, cameraRoot.forward, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            return;
        }

        ArkTree tree = hit.collider.GetComponentInParent<ArkTree>();
        if (tree != null && ArkGameManager.Instance != null)
        {
            tree.Hit(ArkGameManager.Instance.AxeDamage);
        }
    }

    private static Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue() * 0.02f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    private static Vector3 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float x = 0f;
            float z = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            return new Vector3(x, 0f, z);
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
#else
        return Vector3.zero;
#endif
    }

    private static bool IsSprinting()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.eKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

    private static bool WasPrimaryPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private static bool WasSecondaryPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.rKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }
}
