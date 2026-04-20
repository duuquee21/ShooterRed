using Fusion;
using UnityEngine;

// RequireComponent obliga a que este GameObject tenga un CharacterController
// Si no lo tiene, Unity lo aÃ±ade automÃ¡ticamente al aÃ±adir este script
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;          // velocidad horizontal en m/s
    [SerializeField] private float jumpForce = 5f;          // fuerza inicial del salto
    [SerializeField] private float gravity = -20f;          // gravedad aplicada cada tick

    [Header("Vista")]
    [SerializeField] private Transform cameraHolder;        // GameObject vacÃ­o padre de la cÃ¡mara
    [SerializeField] private float mouseSensitivity = 2f;   // multiplicador del movimiento del ratÃ³n
    [SerializeField] private float verticalAngleLimit = 80f;// mÃ¡ximo Ã¡ngulo arriba/abajo (evita girar 360)

    private CharacterController _cc;
    private float _verticalVelocity; // velocidad vertical actual (positiva = subiendo, negativa = cayendo)
    private float _verticalLook;     // Ã¡ngulo acumulado de mirada vertical (para clampear)
    private float _yaw;              // Ã¡ngulo acumulado de rotaciÃ³n horizontal del cuerpo

    // Spawned() se ejecuta cuando este NetworkObject aparece en la sesiÃ³n de Fusion
    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();
        // Guardamos la rotaciÃ³n inicial para no empezar desde 0 si el jugador spawnea rotado
        _yaw = transform.eulerAngles.y;

        // Solo bloqueamos el cursor en el cliente que controla este avatar
        if (HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Update se ejecuta cada frame â€” aquÃ­ solo procesamos input de ratÃ³n
    // No movemos el cuerpo aquÃ­ porque Fusion sobreescribirÃ­a la posiciÃ³n en el siguiente tick
    private void Update()
    {
        // Solo procesa input el cliente que controla este avatar
        if (!HasInputAuthority)
            return;

        // Escape = libera el cursor (Ãºtil para alt-tab o pausar)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click izquierdo = vuelve a bloquear el cursor si se habÃ­a soltado
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
    }

    // FixedUpdateNetwork se ejecuta en cada tick de red de Fusion (determinista)
    // AquÃ­ va todo lo que afecta a la posiciÃ³n del jugador en el mundo compartido
    public override void FixedUpdateNetwork()
    {
        // Solo mueve el cliente que controla este avatar
        if (!HasInputAuthority)
            return;

        // Si el jugador estÃ¡ muerto (Health <= 0), no puede moverse
        if (GameState.TryGetInstance(out GameState gs) &&
            gs.TryGetPlayerData(Object.InputAuthority, out PlayerCombatData data) &&
            data.Health <= 0)
            return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        // Aplicamos el yaw acumulado en Update al transform real del jugador
        // Esto se hace aquÃ­ y no en Update para que Fusion no lo sobreescriba
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        // Input de movimiento: GetAxisRaw no tiene suavizado (0 o 1, sin inercia)
        float h = Input.GetAxisRaw("Horizontal"); // A/D o flechas
        float v = Input.GetAxisRaw("Vertical");   // W/S o flechas

        // Calculamos direcciÃ³n relativa al frente del jugador y normalizamos para
        // evitar que en diagonal vaya mÃ¡s rÃ¡pido (el vector diagonal tiene longitud ~1.41)
        Vector3 horizontal = (transform.right * h + transform.forward * v).normalized * moveSpeed;

        if (_cc.isGrounded)
        {
            // Un pequeÃ±o valor negativo evita que isGrounded parpadee entre frames
            _verticalVelocity = -2f;

            if (Input.GetKey(KeyCode.Space))
                _verticalVelocity = jumpForce; // aplicamos impulso hacia arriba
        }

        // Gravedad acumulativa: cada tick se vuelve mÃ¡s negativo mientras cae
        _verticalVelocity += gravity * Runner.DeltaTime;

        // Combinamos movimiento horizontal y vertical en un solo vector
        Vector3 move = horizontal + Vector3.up * _verticalVelocity;

        // CharacterController.Move gestiona colisiones automÃ¡ticamente
        _cc.Move(move * Runner.DeltaTime);
    }

    private void HandleMouseLook()
    {
        // GetAxisRaw en Mouse X/Y devuelve el delta del ratÃ³n en este frame
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Acumulamos el giro horizontal para aplicarlo en FixedUpdateNetwork
        // Si lo aplicÃ¡ramos directamente aquÃ­, Fusion lo pisarÃ­a cada tick
        _yaw += mouseX;

        // La mirada vertical solo rota el CameraHolder, nunca el cuerpo completo
        // AsÃ­ el cuerpo siempre queda recto y la cÃ¡mara mira arriba/abajo
        _verticalLook -= mouseY; // restamos porque Unity invierte el eje Y del ratÃ³n
        _verticalLook = Mathf.Clamp(_verticalLook, -verticalAngleLimit, verticalAngleLimit);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(_verticalLook, 0f, 0f);
    }
}
