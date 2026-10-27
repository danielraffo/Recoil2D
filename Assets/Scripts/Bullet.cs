using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 22f;
    public float weakSpeed = 12f;
    public float life = 1.4f;
    public float fatness = 0.12f;   // how wide the hit check is

    Vector2 dir;
    bool weak;
    float timeLeft;
    float mySpeed;

    public void Launch(Vector2 d, bool isWeak)
    {
        dir = d.normalized;
        weak = isWeak;
        mySpeed = isWeak ? weakSpeed : speed;
        timeLeft = isWeak ? life * 0.6f : life;

        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && isWeak) sr.color = new Color(0.49f, 0.53f, 0.64f);
        if (isWeak) transform.localScale *= 0.7f;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) { Destroy(gameObject); return; }

        float step = mySpeed * Time.deltaTime;

        // I'm not using a normal collider here. bullets move like 22 units a second and
        // at that speed they were teleporting straight through small enemies between
        // frames. so instead I sweep a circle along the path I'm about to travel and
        // check what's in the way first.
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, fatness, dir, step);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy e = hits[i].collider.GetComponent<Enemy>();
            if (e == null) continue;      // ignore ammo cells, the player, whatever else

            e.TakeHit(weak, hits[i].point);
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(dir * step);

        float h = 6.6f, w = h * 16f / 9f;
        Vector3 p = transform.position;
        if (p.x < -w || p.x > w || p.y < -h || p.y > h)
            Destroy(gameObject);
    }
}
