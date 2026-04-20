using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Vista")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalAngleLimit = 80f;

    private CharacterController _cc;
    private float _verticalVelocity;
    private float _verticalLook;
    private float _yaw;

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();
        _yaw = transform.eulerAngles.y;

        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Update: solo para el look vertical de la camara local (no necesita sincronizarse)
    private void Update()
    {
        if (!HasInputAuthority)
            return;

        // Liberar cursor con Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
    }

    // FixedUpdateNetwork: movimiento sincronizado con Fusion
    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority)
            return;

        // Bloquear movimiento si el jugador esta muerto
        if (GameState.TryGetInstance(out GameState gs) &&
            gs.TryGetPlayerData(Object.InputAuthority, out PlayerCombatData data) &&
            data.Health <= 0)
            return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        // Aplicar yaw acumulado desde Update
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 horizontal = (transform.right * h + transform.forward * v).normalized * moveSpeed;

        if (_cc.isGrounded)
        {
            _verticalVelocity = -2f;

            if (Input.GetKey(KeyCode.Space))
                _verticalVelocity = jumpForce;
        }

        _verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 move = horizontal + Vector3.up * _verticalVelocity;
        _cc.Move(move * Runner.DeltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Acumular yaw para aplicarlo en FixedUpdateNetwork
        _yaw += mouseX;

        // Vertical: solo rota el pivote de la camara local, nunca el cuerpo
        _verticalLook -= mouseY;
        _verticalLook = Mathf.Clamp(_verticalLook, -verticalAngleLimit, verticalAngleLimit);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(_verticalLook, 0f, 0f);
    }
}