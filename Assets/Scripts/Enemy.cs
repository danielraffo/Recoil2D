using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum Kind { Chaser, Tank, Splitter }

    [Header("what am i")]
    public Kind kind = Kind.Chaser;
    public int hp = 1;
    public float speed = 2.2f;
    public int points = 10;

    [Header("splitter only")]
    public Enemy splitInto;
    public int generation = 0;        // stops splitters splitting forever

    [Header("look")]
    public SpriteRenderer body;
    public TMPro.TextMeshPro hpLabel; // only the tank uses this

    Transform target;
    Vector2 vel;
    Color baseColor;
    float flash;

    void Start()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body != null) baseColor = body.color;

        Shooter s = FindFirstObjectByType<Shooter>();
        if (s != null) target = s.transform;

        UpdateLabel();
    }

    void Update()
    {
        if (GameBrain.Instance != null && !GameBrain.Instance.Playing) return;

        if (target != null)
        {
            // just drive straight at the player. nothing clever, they're dumb on purpose
            Vector2 want = ((Vector2)target.position - (Vector2)transform.position).normalized * speed;
            vel = Vector2.Lerp(vel, want, 4f * Time.deltaTime);
            transform.position += (Vector3)(vel * Time.deltaTime);
        }

        // little white blink when I get shot
        if (flash > 0f)
        {
            flash -= Time.deltaTime;
            if (body != null) body.color = (flash > 0f) ? Color.white : baseColor;
        }

        // tanks spin slowly, looks better than nothing
        if (kind == Kind.Tank) transform.Rotate(0, 0, 25f * Time.deltaTime);
    }

    public void TakeHit(bool weakShot, Vector2 where)
    {
        flash = 0.1f;

        // junk shot can kill the normal stuff but tanks don't care
        if (weakShot && kind == Kind.Tank)
        {
            if (GameBrain.Instance != null) GameBrain.Instance.Sparks(where, baseColor, 2);
            return;
        }

        hp--;
        UpdateLabel();

        if (GameBrain.Instance != null)
        {
            GameBrain.Instance.Sparks(where, baseColor, 5);
            GameBrain.Instance.PlayHit();
        }

        if (hp <= 0) Die();
    }

    void Die()
    {
        if (GameBrain.Instance != null)
        {
            GameBrain.Instance.AddScore(points);
            GameBrain.Instance.Sparks(transform.position, baseColor, 16);
            GameBrain.Instance.Shook(0.18f);
            GameBrain.Instance.PlayBoom();
            GameBrain.Instance.MaybeDropCell(transform.position);
        }

        // splitters break in half, but only once. letting the babies split too
        // turned one enemy into like 30 and the game died
        if (kind == Kind.Splitter && splitInto != null && generation < 1)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 off = new Vector3(i == 0 ? -0.35f : 0.35f, 0f, 0f);
                Enemy baby = Instantiate(splitInto, transform.position + off, Quaternion.identity);
                baby.generation = generation + 1;
                baby.transform.localScale = transform.localScale * 0.65f;
                baby.speed = speed * 1.25f;
                baby.points = Mathf.Max(5, points / 2);
                baby.splitInto = null;   // extra safety so these definitely can't split
            }
        }

        Destroy(gameObject);
    }

    void UpdateLabel()
    {
        if (hpLabel == null) return;
        hpLabel.text = (kind == Kind.Tank && hp > 0) ? hp.ToString() : "";
    }
}
