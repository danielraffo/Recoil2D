using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// runs everything: waves, score, the UI text, sounds, screen shake.
// other scripts find this with GameBrain.Instance
public class GameBrain : MonoBehaviour
{
    public static GameBrain Instance;

    [Header("prefabs")]
    public Enemy chaserPrefab;
    public Enemy tankPrefab;
    public Enemy splitterPrefab;
    public AmmoCell cellPrefab;
    public GameObject sparkPrefab;

    [Header("ui")]
    public TMP_Text scoreText;
    public TMP_Text bestText;
    public TMP_Text waveText;
    public TMP_Text ammoText;
    public TMP_Text bigText;      // title screen / dead screen in the middle
    public Image ammoFill;
    public GameObject overlayPanel;

    [Header("waves")]
    public float gapBetweenSpawns = 0.95f;
    public float breatherBetweenWaves = 1.75f;

    [Header("drops")]
    [Range(0f, 1f)] public float dropChance = 0.72f;

    [Header("sounds")]
    public AudioClip shotSfx;
    public AudioClip weakShotSfx;
    public AudioClip hitSfx;
    public AudioClip boomSfx;
    public AudioClip pickupSfx;
    public AudioClip waveSfx;
    public AudioClip deathSfx;

    // state
    public bool Playing { get; private set; }
    int score, best, wave;
    int leftToSpawn;
    float spawnTimer;
    bool betweenWaves;
    Shooter player;
    Camera cam;
    Vector3 camHome;
    float shake;
    float dryTimer;
    AudioSource speaker;
    bool paused;
    bool muted;

    const string BEST_KEY = "recoil_best";

    // the board is always 16:9 even if the Game window is wider
    const float ARENA_H = 6.6f;
    const float ARENA_W = ARENA_H * 16f / 9f;

    void Awake()
    {
        Instance = this;
        speaker = GetComponent<AudioSource>();
        if (speaker == null) speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        LoadSounds();
    }

    void LoadSounds()
    {
        if (shotSfx == null) shotSfx = Resources.Load<AudioClip>("Sounds/shot");
        if (weakShotSfx == null) weakShotSfx = Resources.Load<AudioClip>("Sounds/weakshot");
        if (hitSfx == null) hitSfx = Resources.Load<AudioClip>("Sounds/hit");
        if (boomSfx == null) boomSfx = Resources.Load<AudioClip>("Sounds/boom");
        if (pickupSfx == null) pickupSfx = Resources.Load<AudioClip>("Sounds/pickup");
        if (waveSfx == null) waveSfx = Resources.Load<AudioClip>("Sounds/wave");
        if (deathSfx == null) deathSfx = Resources.Load<AudioClip>("Sounds/death");
    }

    void Start()
    {
        cam = Camera.main;
        camHome = cam.transform.position;
        player = FindFirstObjectByType<Shooter>();

        best = PlayerPrefs.GetInt(BEST_KEY, 0);
        score = 0;
        wave = 0;
        Playing = false;

        Time.timeScale = 1f;
        ShowBig("RE<color=#F2C14E>COIL</color>\n\n<size=42%><color=#7C86A2>shooting is the only way to move</color>\n\naim with the mouse   hold left click to fire\nspace to start</size>");
        Refresh();
    }

    void Update()
    {
        // menu keys
        if (InputStuff.MPressed()) ToggleMute();
        if (InputStuff.EscPressed() && Playing) TogglePause();

        if (!Playing && InputStuff.SpacePressed())
        {
            if (player != null && player.dead) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            else BeginGame();
        }

        if (Playing && !paused) RunWaves();

        DoShake();
        Refresh();
    }

    void BeginGame()
    {
        Playing = true;
        HideBig();
        NextWave();
    }

    // ---------- waves ----------
    void RunWaves()
    {
        if (betweenWaves)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f) { betweenWaves = false; NextWave(); }
            return;
        }

        if (leftToSpawn > 0)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = gapBetweenSpawns;
                leftToSpawn--;
                SpawnOne();
            }
        }
        else if (CountEnemies() == 0)
        {
            // wave cleared, short breather then the next one
            betweenWaves = true;
            spawnTimer = breatherBetweenWaves;
        }

        // mercy drop. if you're totally dry and there's nothing to grab, give you one
        // nearby so you can crawl over with the junk shot instead of just dying
        if (player != null && !player.dead && player.ammo <= 0)
        {
            dryTimer += Time.deltaTime;
            if (dryTimer > 1.25f && FindObjectsByType<AmmoCell>(FindObjectsSortMode.None).Length == 0)
            {
                dryTimer = 0f;
                float a = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(1.7f, 2.7f);
                Vector3 spot = player.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * dist;
                Instantiate(cellPrefab, ClampToScreen(spot), Quaternion.identity);
            }
        }
        else dryTimer = 0f;
    }

    void NextWave()
    {
        wave++;
        leftToSpawn = 2 + Mathf.CeilToInt(wave * 1.0f);
        spawnTimer = 0.55f;
        PlayClip(waveSfx, 0.6f);
    }

    void SpawnOne()
    {
        // tanks show up on wave 2, splitters on 3. keeps the start easy
        List<Enemy> pool = new List<Enemy>();
        pool.Add(chaserPrefab);
        if (wave >= 2) { pool.Add(chaserPrefab); pool.Add(tankPrefab); }
        if (wave >= 3) pool.Add(splitterPrefab);

        Enemy pick = pool[Random.Range(0, pool.Count)];

        // spawn right on the inside edge of the actual board. before this I used
        // the camera width, so on a wide Game window enemies showed up in the side margins
        float pad = 0.45f;
        float left = -ARENA_W + pad, right = ARENA_W - pad;
        float bottom = -ARENA_H + pad, top = ARENA_H - pad;
        Vector3 spot;
        int side = Random.Range(0, 4);
        if (side == 0)      spot = new Vector3(Random.Range(left, right), top, 0f);
        else if (side == 1) spot = new Vector3(right, Random.Range(bottom, top), 0f);
        else if (side == 2) spot = new Vector3(Random.Range(left, right), bottom, 0f);
        else                spot = new Vector3(left, Random.Range(bottom, top), 0f);

        Enemy e = Instantiate(pick, spot, Quaternion.identity);

        // everything gets a little faster every wave, that's the difficulty curve
        e.speed += Mathf.Max(0, wave - 1) * 0.09f;
    }

    int CountEnemies()
    {
        return FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
    }

    Vector3 ClampToScreen(Vector3 p)
    {
        float pad = 0.8f;
        p.x = Mathf.Clamp(p.x, -ARENA_W + pad, ARENA_W - pad);
        p.y = Mathf.Clamp(p.y, -ARENA_H + pad, ARENA_H - pad);
        p.z = 0f;
        return p;
    }

    // ---------- things other scripts call ----------
    public void AddScore(int n)
    {
        score += n;
        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt(BEST_KEY, best);
        }
    }

    public void MaybeDropCell(Vector3 where)
    {
        if (cellPrefab != null && Random.value < dropChance)
            Instantiate(cellPrefab, ClampToScreen(where), Quaternion.identity);
    }

    public void Shook(float amount)
    {
        shake = Mathf.Min(shake + amount, 0.45f);
    }

    public void Sparks(Vector3 where, Color c, int howMany)
    {
        if (sparkPrefab == null) return;
        for (int i = 0; i < howMany; i++)
        {
            GameObject g = Instantiate(sparkPrefab, where, Quaternion.identity);
            Spark s = g.GetComponent<Spark>();
            if (s != null) s.Go(c);
        }
    }

    public void PlayerDied(Vector3 where)
    {
        Playing = false;
        Sparks(where, new Color(0.95f, 0.76f, 0.31f), 30);
        Shook(0.4f);
        PlayClip(deathSfx, 1f);
        PlayerPrefs.Save();
        StartCoroutine(DeathSlowMo());
    }

    IEnumerator DeathSlowMo()
    {
        // quick slow motion on death, it just feels better than cutting straight to the text
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(0.55f);
        Time.timeScale = 1f;
        ShowBig("you ran out of moves\n\n<size=55%>score " + score + "   ·   wave " + wave +
                "\nspace to go again</size>");
    }

    // ---------- sound ----------
    public void PlayShot(bool weak) { PlayClip(weak ? weakShotSfx : shotSfx, weak ? 0.5f : 0.75f); }
    public void PlayHit()           { PlayClip(hitSfx, 0.6f); }
    public void PlayBoom()          { PlayClip(boomSfx, 0.8f); }
    public void PlayPickup()        { PlayClip(pickupSfx, 0.7f); }

    void PlayClip(AudioClip clip, float vol)
    {
        if (clip == null || speaker == null) return;
        // tiny pitch wobble so rapid fire doesn't sound like a machine
        speaker.pitch = Random.Range(0.94f, 1.06f);
        speaker.PlayOneShot(clip, vol);
    }

    void ToggleMute()
    {
        muted = !muted;
        AudioListener.volume = muted ? 0f : 1f;
    }

    void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (paused) ShowBig("paused\n\n<size=55%>esc to keep going</size>");
        else HideBig();
    }

    // ---------- camera + ui ----------
    void DoShake()
    {
        if (shake > 0f)
        {
            shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime * 1.6f);
            Vector3 jiggle = new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0f);
            cam.transform.position = camHome + jiggle;
        }
        else cam.transform.position = camHome;
    }

    void Refresh()
    {
        if (scoreText != null) scoreText.text = "score " + score;
        if (bestText != null)  bestText.text  = "best " + best;
        if (waveText != null)  waveText.text  = Playing ? ("wave " + wave) : "";

        if (ammoText != null && player != null)
        {
            if (player.ammo <= 0)
                ammoText.text = "<color=#E5484D>ammo 0 - junk shot only, grab a cell</color>";
            else
                ammoText.text = "ammo " + player.ammo;

            if (ammoFill != null)
            {
                ammoFill.fillAmount = (float)player.ammo / player.maxAmmo;
                ammoFill.color = player.ammo <= 2 ? new Color(0.898f, 0.282f, 0.302f) : new Color(0.949f, 0.757f, 0.306f);
            }
        }
    }

    void ShowBig(string msg)
    {
        if (bigText == null) return;
        bigText.text = msg;
        if (overlayPanel != null) overlayPanel.SetActive(true);
        bigText.gameObject.SetActive(true);
    }

    void HideBig()
    {
        if (bigText != null) bigText.gameObject.SetActive(false);
        if (overlayPanel != null) overlayPanel.SetActive(false);
    }
}
