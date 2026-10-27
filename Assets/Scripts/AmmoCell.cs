using UnityEngine;

// the blue things enemies drop. ammo is also your gas tank so these matter a lot
public class AmmoCell : MonoBehaviour
{
    public int gives = 7;
    public float life = 12f;
    public float spinSpeed = 140f;

    Vector2 drift;
    SpriteRenderer sr;
    Vector3 baseScale;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
        // little random scoot so they don't all sit in a pile
        drift = new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(-1.4f, 1.4f));
    }

    void Update()
    {
        if (GameBrain.Instance != null && !GameBrain.Instance.Playing) return;

        transform.position += (Vector3)(drift * Time.deltaTime);
        drift *= 0.97f;

        // don't let pickups slowly drift outside the board
        float h = 6.6f, w = h * 16f / 9f, pad = 0.45f;
        Vector3 p = transform.position;
        if (p.x < -w + pad || p.x > w - pad) drift.x *= -0.55f;
        if (p.y < -h + pad || p.y > h - pad) drift.y *= -0.55f;
        p.x = Mathf.Clamp(p.x, -w + pad, w - pad);
        p.y = Mathf.Clamp(p.y, -h + pad, h - pad);
        transform.position = p;
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
        transform.localScale = baseScale * pulse;

        life -= Time.deltaTime;

        // blink when it's about to disappear, warns you to hurry up
        if (sr != null && life < 3f)
        {
            Color c = sr.color;
            c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(life * 8f));
            sr.color = c;
        }

        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Shooter s = other.GetComponent<Shooter>();
        if (s == null || s.dead) return;

        s.AddAmmo(gives);
        if (GameBrain.Instance != null)
        {
            GameBrain.Instance.Sparks(transform.position, new Color(0.42f, 0.84f, 1f), 10);
            GameBrain.Instance.PlayPickup();
        }
        Destroy(gameObject);
    }
}
