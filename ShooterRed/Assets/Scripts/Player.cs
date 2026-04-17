using Fusion;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float mouseSensitivity = 50f; // Ajusta para velocidad de mouse
    public float gravity = -9.81f;

    [Header("Shooting")]
    public float shootRange = 100f;
    public int damage = 10;
    public float shootRate = 5f; // shots per second
    public LayerMask hitLayers;
    public Color tracerColor = Color.red;
    public float tracerWidth = 0.1f;
    public float tracerDuration = 0.08f;
    public Color muzzleColor = Color.yellow;
    public float muzzleSize = 0.5f;
    public float muzzleDuration = 0.1f;

    [Header("Health")]
    [Networked] public int Health { get; set; } = 100;
    public int maxHealth = 100;

    [Header("UI")]
    public int crosshairSize = 20;
    public Color crosshairColor = Color.red;

    private CharacterController controller;
    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    private LineRenderer tracer;
    private float tracerTimer;
    private GameObject muzzleFlash;
    private float muzzleTimer;
    private float nextShootTime = 0f;
    private bool isAimingTracer = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        
        if (controller == null)
            Debug.LogError("Player: No CharacterController found!");
        if (cam == null)
            Debug.LogWarning("Player: No Camera found in children!");

        CreateTracer();
        CreateMuzzleFlash();
        Debug.Log("Player spawned at position: " + transform.position);
    }

    private void CreateTracer()
    {
        var tracerGO = new GameObject("ShotTracer");
        tracerGO.transform.SetParent(transform);
        tracerGO.transform.localPosition = Vector3.zero;

        tracer = tracerGO.AddComponent<LineRenderer>();
        tracer.positionCount = 2;
        tracer.useWorldSpace = true;
        tracer.startWidth = Mathf.Max(tracerWidth, 0.08f);
        tracer.endWidth = Mathf.Max(tracerWidth, 0.08f);
        tracer.numCornerVertices = 4;
        tracer.numCapVertices = 4;
        tracer.material = new Material(Shader.Find("Sprites/Default"));
        tracer.startColor = tracerColor;
        tracer.endColor = tracerColor;
        tracer.alignment = LineAlignment.View;
        tracer.enabled = false;
    }

    private void CreateMuzzleFlash()
    {
        muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleFlash.name = "MuzzleFlash";
        muzzleFlash.transform.SetParent(cam != null ? cam.transform : transform);
        muzzleFlash.transform.localScale = Vector3.one * muzzleSize;
        muzzleFlash.transform.localPosition = new Vector3(0f, -0.1f, 0.5f);
        Destroy(muzzleFlash.GetComponent<Collider>());

        var renderer = muzzleFlash.GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Unlit/Color"));
        renderer.material.color = muzzleColor;
        muzzleFlash.SetActive(false);
    }

    private void Update()
    {
        if (tracer != null && tracer.enabled && !isAimingTracer)
        {
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0f)
            {
                tracer.enabled = false;
            }
        }

        if (muzzleFlash != null && muzzleFlash.activeSelf)
        {
            muzzleTimer -= Time.deltaTime;
            if (muzzleTimer <= 0f)
            {
                muzzleFlash.SetActive(false);
            }
        }
    }

    private void OnGUI()
    {
        if (!HasInputAuthority) return;

        int size = crosshairSize;
        int x = Screen.width / 2 - size / 2;
        int y = Screen.height / 2 - size / 2;
        GUI.color = crosshairColor;
        GUI.Label(new Rect(x, y, size, size), "+");
    }

    public override void Spawned()
    {
        Debug.Log($"Player spawned! HasInputAuthority: {HasInputAuthority}");
        
        if (HasInputAuthority)
        {
            // Enable camera for local player
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                cam.tag = "MainCamera";
            }
        }
        else
        {
            // Disable camera for remote players
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            Debug.Log($"Input received: H={input.Horizontal}, V={input.Vertical}, Shoot={input.Shoot}");
            
            // Movement
            Vector3 moveDirection;
            if (HasInputAuthority && cam != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                moveDirection = (forward * input.Vertical + right * input.Horizontal).normalized;
            }
            else
            {
                moveDirection = new Vector3(input.Horizontal, 0, input.Vertical).normalized;
            }

            if (HasInputAuthority && cam != null)
            {
                Vector3 lookDirection = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (lookDirection.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), rotationSpeed * Runner.DeltaTime);
                }
            }
            
            // Apply gravity
            velocity.y += gravity * Runner.DeltaTime;
            
            // Move the character
            Vector3 moveVelocity = (moveDirection * moveSpeed) + new Vector3(0, velocity.y, 0);
            
            if (controller != null)
            {
                controller.Move(moveVelocity * Runner.DeltaTime);
                Debug.Log($"Moving with velocity: {moveVelocity}");
            }
            else
            {
                Debug.LogError("CharacterController is NULL!");
            }
            
            // Ground detection - reset vertical velocity when touching ground
            if (controller != null && controller.isGrounded)
            {
                velocity.y = 0;
            }

            // Camera rotation with mouse
            if (HasInputAuthority && cam != null)
            {
                float mouseX = input.MouseX * mouseSensitivity * Runner.DeltaTime;
                float mouseY = input.MouseY * mouseSensitivity * Runner.DeltaTime;
                
                // Yaw: rotate player horizontally
                transform.Rotate(Vector3.up, mouseX);
                
                // Pitch: rotate camera vertically
                cam.transform.Rotate(Vector3.left, mouseY);

                // Clamp vertical rotation
                Vector3 euler = cam.transform.localEulerAngles;
                euler.x = Mathf.Clamp(euler.x > 180 ? euler.x - 360 : euler.x, -90, 90);
                cam.transform.localEulerAngles = euler;
            }

            // Shooting
            bool shot = false;
            if (input.Shoot && input.Aim && Runner.SimulationTime >= nextShootTime)
            {
                Shoot();
                shot = true;
            }

            // Aiming ray
            if (input.Aim && !shot && HasInputAuthority && cam != null)
            {
                ShowAimRay();
            }
            else if (!input.Aim && tracerTimer <= 0)
            {
                tracer.enabled = false;
            }
            else if (!input.Aim && tracer != null && tracer.enabled)
            {
                tracer.enabled = false;
            }
        }
        else
        {
            if (HasInputAuthority)
            {
                Debug.LogWarning("GetInput returned false!");
            }
        }
    }

    private void Shoot()
    {
        if (cam == null) return;

        LayerMask mask = hitLayers.value == 0 ? Physics.DefaultRaycastLayers : hitLayers;
        Ray ray = new Ray(transform.position, cam.transform.forward);
        Vector3 endPoint = ray.origin + ray.direction * shootRange;

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange, mask))
        {
            endPoint = hit.point;
            Player targetPlayer = hit.collider.GetComponentInParent<Player>();
            if (targetPlayer != null && targetPlayer != this)
            {
                Debug.Log($"Shot hit player: {targetPlayer.name}");
                targetPlayer.TakeDamageRpc(damage);
            }

            DestructibleCube cube = hit.collider.GetComponent<DestructibleCube>();
            if (cube != null)
            {
                cube.TakeHitRpc();
            }

            CreateImpact(hit.point);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * shootRange, Color.red, 0.1f);
        }

        ShowTracer(ray.origin, endPoint);
        ShowMuzzleFlash();
        nextShootTime = Runner.SimulationTime + (1f / shootRate);
    }

    private void ShowMuzzleFlash()
    {
        if (muzzleFlash == null) return;

        muzzleFlash.SetActive(true);
        muzzleTimer = muzzleDuration;
    }

    private void ShowTracer(Vector3 start, Vector3 end)
    {
        if (tracer == null) return;

        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        tracerTimer = tracerDuration;
        tracer.enabled = true;
        tracer.widthMultiplier = Mathf.Max(tracerWidth, 0.08f);
        isAimingTracer = false;
    }

    private void ShowAimRay()
    {
        if (cam == null || tracer == null) return;

        LayerMask mask = hitLayers.value == 0 ? Physics.DefaultRaycastLayers : hitLayers;
        Ray ray = new Ray(transform.position, cam.transform.forward);
        Vector3 endPoint = ray.origin + ray.direction * shootRange;

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange, mask))
        {
            endPoint = hit.point;
        }

        tracer.SetPosition(0, ray.origin);
        tracer.SetPosition(1, endPoint);
        tracer.enabled = true;
        tracer.widthMultiplier = tracerWidth;
        isAimingTracer = true;
    }

    private void CreateImpact(Vector3 position)
    {
        GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.transform.position = position;
        impact.transform.localScale = Vector3.one * 0.08f;
        Destroy(impact.GetComponent<Collider>());
        Material material = new Material(Shader.Find("Unlit/Color"));
        material.color = Color.yellow;
        impact.GetComponent<MeshRenderer>().material = material;
        Destroy(impact, 0.15f);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void TakeDamageRpc(int amount)
    {
        if (!HasStateAuthority) return;

        Health -= amount;
        Debug.Log($"Player took {amount} damage! Health: {Health}");

        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Respawn
        Health = maxHealth;
        transform.position = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        velocity = Vector3.zero;
        Debug.Log("Player respawned!");
    }
}

public struct NetworkInputData : INetworkInput
{
    public float Horizontal;
    public float Vertical;
    public NetworkBool Shoot;
    public NetworkBool Aim;
    public float MouseX;
    public float MouseY;
}