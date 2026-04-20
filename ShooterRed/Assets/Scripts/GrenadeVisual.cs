using UnityEngine;

// Script puramente local — NO hereda de NetworkBehaviour, NO necesita NetworkObject.
// Simula la física visual de la granada. Cuando explota, llama a OnExplode con la posición final.
public class GrenadeVisual : MonoBehaviour
{
    [SerializeField] private float fuseTime = 2.5f;
    [SerializeField] private float bounceDamping = 0.4f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float explosionRadius = 8f; // debe coincidir con el radio en GameState

    // Callback que PlayerCombatIntent suscribe para recibir la posición de explosión
    public System.Action<Vector3> OnExplode;

    private Vector3 _velocity;
    private float _spawnTime;

    public void Launch(Vector3 initialVelocity)
    {
        _velocity = initialVelocity;
        _spawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - _spawnTime >= fuseTime)
        {
            // Muestra el aro visual de explosión en el punto de aterrizaje
            ExplosionVisual.Spawn(transform.position, explosionRadius);
            // Notifica la posición final antes de destruirse
            OnExplode?.Invoke(transform.position);
            Destroy(gameObject);
            return;
        }

        _velocity.y += gravity * Time.deltaTime;
        float step = _velocity.magnitude * Time.deltaTime;

        if (step > 0f && Physics.Raycast(transform.position, _velocity.normalized, out RaycastHit hit, step + 0.1f))
        {
            transform.position = hit.point + hit.normal * 0.05f;
            _velocity = Vector3.Reflect(_velocity, hit.normal) * bounceDamping;
        }
        else
        {
            transform.position += _velocity * Time.deltaTime;
        }

        transform.Rotate(Vector3.right, _velocity.magnitude * 200f * Time.deltaTime, Space.Self);
    }
}
