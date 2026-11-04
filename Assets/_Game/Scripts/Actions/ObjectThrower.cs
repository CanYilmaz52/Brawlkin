using UnityEngine;

public class ObjectThrower2D : MonoBehaviour
{
    [Header("Tuş (Inspector'dan seç)")]
    public KeyCode throwKey = KeyCode.F;
    [Tooltip("Açıksa tuşu bu script kendisi dinler. Kendi Player scriptinden Throw() çağıracaksan kapat.")]
    public bool useOwnInput = true;

    [Header("Fırlatılacak Obje")]
    [Tooltip("Rigidbody2D'si olan bir prefab")]
    public Rigidbody2D projectilePrefab;
    [Tooltip("Objenin çıkış noktası (karakterin elinde boş bir child obje)")]
    public Transform spawnPoint;

    [Header("Fırlatma Ayarları")]
    public float throwSpeed = 12f;
    [Tooltip("Yukarı doğru açı (derece). 0 = düz, 30 = hafif yay çizer")]
    public float throwAngle = 20f;
    public float cooldown = 0.4f;
    [Tooltip("Oyuncunun hızını objeye de ekle")]
    public bool inheritShooterVelocity = false;

    [Header("Opsiyonel")]
    [Tooltip("Boş bırakırsan yön, objenin localScale.x işaretinden alınır")]
    public PlayerController2D player;

    float nextThrowTime;

    void Update()
    {
        if (useOwnInput && Input.GetKeyDown(throwKey))
            Throw();
    }

    // Kendi scriptinden: thrower.Throw();
    public void Throw()
    {
        int dir;
        if (player != null) dir = player.FacingDirection;
        else dir = transform.lossyScale.x >= 0 ? 1 : -1;

        Throw(dir);
    }

    // Yönü sen vermek istersen: thrower.Throw(-1);
    public void Throw(int direction)
    {
        if (projectilePrefab == null || Time.time < nextThrowTime) return;
        nextThrowTime = Time.time + cooldown;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Rigidbody2D proj = Instantiate(projectilePrefab, pos, Quaternion.identity);

        // Objeyi gittiği yöne çevir
        Vector3 s = proj.transform.localScale;
        s.x = Mathf.Abs(s.x) * direction;
        proj.transform.localScale = s;

        float rad = throwAngle * Mathf.Deg2Rad;
        Vector2 vel = new Vector2(Mathf.Cos(rad) * direction, Mathf.Sin(rad)) * throwSpeed;

        if (inheritShooterVelocity)
        {
            Rigidbody2D shooter = GetComponent<Rigidbody2D>();
            if (shooter != null) vel += shooter.linearVelocity;
        }

        proj.linearVelocity = vel;
    }
}