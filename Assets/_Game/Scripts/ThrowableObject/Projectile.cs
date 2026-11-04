using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile2D : MonoBehaviour
{
    public float lifetime = 3f;
    [Tooltip("Havada dönme hızı (derece/sn). 0 = dönmez")]
    public float spinSpeed = 360f;
    [Tooltip("Çarpınca yok olsun mu")]
    public bool destroyOnHit = true;
    [Tooltip("Bu katmanlara çarpınca yok olur (Ground, Enemy vb.)")]
    public LayerMask hitLayers = ~0;
    public int damage = 10;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (spinSpeed != 0f)
            rb.MoveRotation(rb.rotation - Mathf.Sign(transform.localScale.x) * spinSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionEnter2D(Collision2D col) { Hit(col.collider); }
    void OnTriggerEnter2D(Collider2D other) { Hit(other); }

    void Hit(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

        // Hasar sistemin varsa burada çağır, örnek:
        // other.GetComponent<Health>()?.TakeDamage(damage);

        if (destroyOnHit) Destroy(gameObject);
    }
}