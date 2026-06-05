using System.Collections;
using UnityEngine;

/// <summary>
/// Self-contained boss controller.
/// Movement is handled entirely here — disable any movement logic
/// inside EnemyScript for the boss prefab (EnemyScript is still needed
/// for lazerShot / lazerGun references).
///
/// PHASES (based on remaining HP %):
///   Phase 1 – 100 % → 60 %  slow orbit, 5-shot burst
///   Phase 2 –  60 % → 30 %  faster orbit + charge attacks, 7-shot burst
///   Phase 3 –  30 % →  0 %  very fast orbit + frequent charges, 9-shot burst
/// </summary>
public class BossScript : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Health")]
    [Min(1)] public int hitsToKill = 35;

    [Header("References")]
    public GameObject explosionPrefab;

    [Header("Hit Feedback")]
    [SerializeField] Color hitFlashColor = new Color(0.65f, 0.95f, 1f, 1f);
    [SerializeField] float hitFlashDuration = 0.14f;

    [Header("Movement")]
    [SerializeField] float moveSpeed        = 18f;   // Horizontal tracking speed
    [SerializeField] float xOffsetAmplitude = 6f;    // Side-to-side weave around player X
    [SerializeField] float combatZ          = 42f;   // Visible combat line on screen

    [Header("Phase 1 Shooting (100–60 % HP)")]
    [SerializeField] float  p1Cooldown    = 1.5f;
    [SerializeField] int    p1ShotCount   = 5;
    [SerializeField] float  p1Spread      = 28f;
    [SerializeField] float  p1ShotSpeed   = 60f;
    [SerializeField] int    p1ShotDamage  = 3;

    [Header("Phase 2 Shooting (60–30 % HP)")]
    [SerializeField] float  p2Cooldown    = 1.05f;
    [SerializeField] int    p2ShotCount   = 7;
    [SerializeField] float  p2Spread      = 38f;

    [Header("Phase 3 Shooting (30–0 % HP)")]
    [SerializeField] float  p3Cooldown    = 0.7f;
    [SerializeField] int    p3ShotCount   = 9;
    [SerializeField] float  p3Spread      = 50f;

    // ── Internal state ─────────────────────────────────────────────────────

    int   hitsLeft;
    bool  isDead;

    // Movement
    float orbitDir = 1f;
    float fixedBossZ;

    // Shooting
    float nextBurstTime;

    // Phases
    int phase = 1;

    // Hit feedback
    SpriteRenderer[] spriteRenderers;
    Color[] originalSpriteColors;
    Renderer[] meshRenderers;
    Color[] originalRendererColors;
    bool[] rendererHasColor;
    Coroutine hitFlashCoroutine;

    // Arena limits
    const float ArenaHalfX   = 32f;
    const float ArenaMinZ    = -68f;
    const float ArenaMaxZ    = 60f;
    const float BossY        = 2f;    // Fixed height; adjust to match your level

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        hitsLeft        = hitsToKill;
        gameObject.tag  = "Enemy";
        orbitDir        = Random.value > 0.5f ? 1f : -1f;
        fixedBossZ      = Mathf.Clamp(combatZ, ArenaMinZ + 12f, ArenaMaxZ - 8f);
        transform.position = new Vector3(transform.position.x, BossY, fixedBossZ);
        CacheHitRenderers();
    }

    void Update()
    {
        if (isDead) return;
        if (hitsLeft <= 0)
        {
            DieAndWin();
            return;
        }

        RecoverIfOutOfBounds();
        UpdatePhase();
        UpdateMovement();
        TryBurstShoot();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Ignore these entirely
        if (other.CompareTag("GameBoundary")   ||
            other.CompareTag("LazerEnemyShot") ||
            other.CompareTag("PowerUp")        ||
            other.CompareTag("Enemy")          ||
            other.CompareTag("Asteroid"))       return;

        if (other.CompareTag("LazerShot"))
        {
            Destroy(other.gameObject);
            hitsLeft--;
            if (hitsLeft <= 0)
            {
                DieAndWin();
            }
            else
            {
                PlayHitFlash();
            }
            return;
        }

        // Any other collision (e.g. player ship ramming boss) — boss ignores it
    }

    // ── Phase ──────────────────────────────────────────────────────────────

    void UpdatePhase()
    {
        float hp = (float)hitsLeft / hitsToKill;
        int newPhase = hp > 0.6f ? 1 : hp > 0.3f ? 2 : 3;

        if (newPhase != phase)
        {
            phase = newPhase;

            // On phase change: reverse orbit direction for a visible "rage" reaction
            orbitDir *= -1f;
        }
    }

    // ── Movement ───────────────────────────────────────────────────────────

    void UpdateMovement()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null || !playerObj.activeInHierarchy) return;

        Vector3 playerPos = playerObj.transform.position;
        playerPos.y = BossY;

        // Boss moves only on X axis: tracks player and weaves left/right.
        float phaseSpeed = phase == 1 ? 1f : phase == 2 ? 1.2f : 1.4f;
        float weave = Mathf.Sin(Time.time * (phase == 3 ? 2.2f : 1.6f)) * xOffsetAmplitude * orbitDir;
        float targetX = Mathf.Clamp(playerPos.x + weave, -ArenaHalfX, ArenaHalfX);
        float newX = Mathf.MoveTowards(transform.position.x, targetX, moveSpeed * phaseSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, BossY, fixedBossZ);

        // Smoothly face the player
        FacePlayer(playerPos);
    }

    void FacePlayer(Vector3 playerPos)
    {
        Vector3 look = playerPos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f) return;

        float turnSpeed = phase == 1 ? 3f : phase == 2 ? 5f : 7.5f;
        Quaternion target = Quaternion.LookRotation(look);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    void RecoverIfOutOfBounds()
    {
        if (transform.position.z < ArenaMinZ || transform.position.z > ArenaMaxZ)
        {
            transform.position = new Vector3(
                Random.Range(-ArenaHalfX * 0.5f, ArenaHalfX * 0.5f),
                BossY,
                fixedBossZ);
        }
    }

    // ── Shooting ───────────────────────────────────────────────────────────

    void TryBurstShoot()
    {
        if (Time.time < nextBurstTime) return;

        EnemyScript enemy = GetComponent<EnemyScript>();
        if (enemy == null || enemy.lazerShot == null || enemy.lazerGun == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || !player.activeInHierarchy) return;

        // Pick phase params
        float cooldown = phase == 1 ? p1Cooldown : phase == 2 ? p2Cooldown : p3Cooldown;
        int   shots    = phase == 1 ? p1ShotCount : phase == 2 ? p2ShotCount : p3ShotCount;
        float spread   = phase == 1 ? p1Spread    : phase == 2 ? p2Spread    : p3Spread;

        Vector3 dir = player.transform.position - enemy.lazerGun.position;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.back;
        Vector3 forward   = dir.normalized;
        float   half      = spread * 0.5f;
        int     shotCount = Mathf.Max(3, shots);

        for (int i = 0; i < shotCount; i++)
        {
            float   t           = shotCount == 1 ? 0.5f : (float)i / (shotCount - 1);
            float   angle       = Mathf.Lerp(-half, half, t);
            Vector3 shotDir     = Quaternion.AngleAxis(angle, Vector3.up) * forward;

            GameObject shot = Instantiate(enemy.lazerShot,
                                          enemy.lazerGun.position,
                                          Quaternion.LookRotation(shotDir, Vector3.up));

            Rigidbody rb = shot.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = shotDir * p1ShotSpeed;

            DamageSource src = shot.GetComponent<DamageSource>()
                            ?? shot.AddComponent<DamageSource>();
            src.damage = Mathf.Max(1, p1ShotDamage);
        }

        nextBurstTime = Time.time + cooldown;
    }

    // ── Hit feedback ──────────────────────────────────────────────────────

    void CacheHitRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalSpriteColors[i] = spriteRenderers[i].color;
                }
            }
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        int meshRendererCount = 0;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null && !(allRenderers[i] is SpriteRenderer))
            {
                meshRendererCount++;
            }
        }

        if (meshRendererCount == 0)
        {
            return;
        }

        meshRenderers = new Renderer[meshRendererCount];
        originalRendererColors = new Color[meshRendererCount];
        rendererHasColor = new bool[meshRendererCount];

        int meshIndex = 0;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer currentRenderer = allRenderers[i];
            if (currentRenderer == null || currentRenderer is SpriteRenderer)
            {
                continue;
            }

            meshRenderers[meshIndex] = currentRenderer;
            Material material = currentRenderer.material;
            if (material != null && material.HasProperty("_Color"))
            {
                originalRendererColors[meshIndex] = material.color;
                rendererHasColor[meshIndex] = true;
            }

            meshIndex++;
        }
    }

    void PlayHitFlash()
    {
        if (!HasHitRenderers())
        {
            CacheHitRenderers();
        }

        if (!HasHitRenderers())
        {
            return;
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    bool HasHitRenderers()
    {
        bool hasSprites = spriteRenderers != null && spriteRenderers.Length > 0;
        bool hasMeshes = meshRenderers != null && meshRenderers.Length > 0;
        return hasSprites || hasMeshes;
    }

    IEnumerator HitFlashRoutine()
    {
        SetSpriteColors(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreSpriteColors();
        hitFlashCoroutine = null;
    }

    void SetSpriteColors(Color color)
    {
        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = color;
                }
            }
        }

        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] == null ||
                    rendererHasColor == null ||
                    i >= rendererHasColor.Length ||
                    !rendererHasColor[i])
                {
                    continue;
                }

                Material material = meshRenderers[i].material;
                if (material != null && material.HasProperty("_Color"))
                {
                    material.color = color;
                }
            }
        }
    }

    void RestoreSpriteColors()
    {
        if (spriteRenderers != null && originalSpriteColors != null)
        {
            int spriteCount = Mathf.Min(spriteRenderers.Length, originalSpriteColors.Length);
            for (int i = 0; i < spriteCount; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = originalSpriteColors[i];
                }
            }
        }

        if (meshRenderers == null || originalRendererColors == null)
        {
            return;
        }

        int rendererCount = Mathf.Min(meshRenderers.Length, originalRendererColors.Length);
        for (int i = 0; i < rendererCount; i++)
        {
            if (meshRenderers[i] == null ||
                rendererHasColor == null ||
                i >= rendererHasColor.Length ||
                !rendererHasColor[i])
            {
                continue;
            }

            Material material = meshRenderers[i].material;
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = originalRendererColors[i];
            }
        }
    }

    // ── Death ──────────────────────────────────────────────────────────────

    void DieAndWin()
    {
        if (isDead) return;
        isDead = true;

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        if (GameControllerScript.instance != null)
            GameControllerScript.instance.ShowVictory();

        Destroy(gameObject);
    }

    // ── Public helpers (kept for backward compatibility) ───────────────────

    public float GetMinDistanceToPlayer() => 0f;
    public float GetTurnSpeed()           => 4.5f;        // Unused internally now
    public float GetOrbitDirection()      => orbitDir;
}
