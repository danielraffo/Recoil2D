using UnityEngine;

// this is the player. there is no WASD on purpose, the gun kick IS the movement.
// most of the numbers up here are stuff I ended up tweaking while playtesting
[RequireComponent(typeof(Rigidbody2D))]
public class Shooter : MonoBehaviour
{
    [Header("feel stuff - tweak these while playing")]
    public float recoilForce = 6.5f;   // how hard each shot shoves me
    public float maxSpeed = 15f;       // stops a fast burst launching me into space
    public float slowDown = 2.1f;      // drag, so I coast to a stop instead of forever
    public float fireDelay = 0.14f;

    [Header("ammo")]
    public int startAmmo = 16;
    public int maxAmmo = 28;

    [Header("hook ups")]
    public Bullet bulletPrefab;
    public SpriteRenderer body;        // goes gray when I'm empty
    public LineRenderer aimLine;

    [HideInInspector] public int ammo;
    [HideInInspector] public bool dead;

    Rigidbody2D rb;
    Camera cam;
    float cool;
    float minX, maxX, minY, maxY;
    Color normalColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = slowDown;   // older unity calls this "drag"
        rb.freezeRotation = true;
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body != null) normalColor = body.color;
    }

    void Start()
    {
        cam = Camera.main;
        ammo = startAmmo;
        FigureOutWalls();
    }

    // the visible board is fixed at 16:9. using cam.aspect here let me fly into
    // the empty side area whenever the Game tab was extra wide
    void FigureOutWalls()
    {
        float h = 6.6f;
        float w = h * 16f / 9f;
        float pad = 0.55f;
        minX = -w + pad; maxX = w - pad;
        minY = -h + pad; maxY = h - pad;
    }

    void Update()
    {
        if (dead) return;
        if (GameBrain.Instance != null && !GameBrain.Instance.Playing) return;
        if (Time.timeScale == 0f) return;

        // point at the mouse
        Vector3 mouse = cam.ScreenToWorldPoint(InputStuff.MouseScreenPos());
        mouse.z = 0f;
        Vector2 aim = ((Vector2)mouse - rb.position).normalized;
        if (aim.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);

        if (aimLine != null)
        {
            aimLine.SetPosition(0, transform.position);
            aimLine.SetPosition(1, mouse);
        }

        cool -= Time.deltaTime;
        if (InputStuff.FireHeld() && cool <= 0f) Shoot(aim);

        // gray out when the mag is dry so you can tell at a glance
        if (body != null)
            body.color = (ammo <= 0) ? new Color(0.49f, 0.53f, 0.64f) : normalColor;
    }

    void FixedUpdate()
    {
        if (dead) return;

        // speed cap
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // bounce off the walls, lose some speed doing it
        Vector2 p = rb.position;
        Vector2 v = rb.linearVelocity;
        if (p.x < minX) { p.x = minX; v.x = Mathf.Abs(v.x) * 0.45f; }
        if (p.x > maxX) { p.x = maxX; v.x = -Mathf.Abs(v.x) * 0.45f; }
        if (p.y < minY) { p.y = minY; v.y = Mathf.Abs(v.y) * 0.45f; }
        if (p.y > maxY) { p.y = maxY; v.y = -Mathf.Abs(v.y) * 0.45f; }
        rb.position = p;
        rb.linearVelocity = v;
    }

    void Shoot(Vector2 aim)
    {
        // out of ammo doesn't mean stuck. the junk shot still pushes you around.
        // it can kill the red and teal enemies, but the tank just ignores it
        bool weak = (ammo <= 0);

        Bullet b = Instantiate(bulletPrefab, rb.position + aim * 0.55f, Quaternion.identity);
        b.Launch(aim, weak);

        // THE whole game right here. shot goes one way, I go the other
        rb.AddForce(-aim * recoilForce * (weak ? 0.55f : 1f), ForceMode2D.Impulse);

        if (!weak) ammo--;
        cool = weak ? fireDelay * 2.4f : fireDelay;

        if (GameBrain.Instance != null)
        {
            GameBrain.Instance.Shook(weak ? 0.04f : 0.12f);
            GameBrain.Instance.PlayShot(weak);
        }
    }

    public void AddAmmo(int n)
    {
        ammo = Mathf.Min(maxAmmo, ammo + n);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;
        if (other.GetComponent<Enemy>() == null) return;

        dead = true;
        rb.linearVelocity = Vector2.zero;
        if (GameBrain.Instance != null) GameBrain.Instance.PlayerDied(transform.position);
    }
}
