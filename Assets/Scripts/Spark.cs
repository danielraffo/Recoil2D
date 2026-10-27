using UnityEngine;

// cheap little particle. I spawn a bunch of these on hits and deaths.
// could have used unity's particle system but this was easier to color per enemy
public class Spark : MonoBehaviour
{
    Vector2 vel;
    float life;
    float startLife;
    SpriteRenderer sr;
    Vector3 startScale;

    public void Go(Color c)
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = c;

        float a = Random.Range(0f, Mathf.PI * 2f);
        float s = Random.Range(1.5f, 7f);
        vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s;

        startLife = Random.Range(0.25f, 0.55f);
        life = startLife;
        startScale = transform.localScale;
    }

    void Update()
    {
        if (sr == null) { Destroy(gameObject); return; }

        transform.position += (Vector3)(vel * Time.deltaTime);
        vel *= 0.93f;

        life -= Time.deltaTime;
        float t = Mathf.Clamp01(life / startLife);

        Color c = sr.color;
        c.a = t;
        sr.color = c;
        transform.localScale = startScale * t;

        if (life <= 0f) Destroy(gameObject);
    }
}
