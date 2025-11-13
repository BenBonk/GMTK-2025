using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Animal : MonoBehaviour
{
    private GameManager gameManager;
    private Player player;
    private AnimalLevelManager levelManager;

    public AnimalData animalData;
    public bool isPredator;

    public float speed;
    public float currentSpeed;

    //movement parameters
    public bool isLassoed;
    public float leftEdgeX;
    public Vector3 startPos;
    protected Vector3 externalOffset = Vector3.zero;
    protected Vector3 pendingExternalOffset = Vector3.zero;

    public float topLimitY;
    public float bottomLimitY;

    protected float minimumSpacing = 0.15f;
    public float MinimumSpacing => minimumSpacing;
    private float repelForce = 4; 
    public virtual bool IsRepelImmune => false;
    public virtual bool CanBeLassoed => true;
    protected virtual bool ShouldClampY => attractTarget == null;

    // run animation parameters
    private float tiltAngle = 0f;
    public float tiltFrequency = 3f; // how fast the tilt cycles 
    public float maxTiltAmplitude = 20f; // degrees at full speed

    protected float tiltProgress = 0f;
    private Vector3 previousPosition;
    public float actualSpeed { get; private set; } // total movement speed
    public bool legendary;
    public bool forceExit = false;
    [HideInInspector] public int bonusPoints;
    [HideInInspector] public bool struckByLightning;

    // ---------- global speed scale ----------
    private static float _globalSpeedScale = 1f;
    public static float GlobalSpeedScale => _globalSpeedScale;
    protected float EffectiveSpeed => speed * _effectiveSpeedScale;

    public static event Action<float> OnGlobalSpeedScaleChanged;
    private static event Action OnAnyGlobalSpeedFactorChanged;
    public static void SetGlobalSpeedScale(float scale)
    {
        scale = Mathf.Max(0.0001f, scale);
        if (Mathf.Approximately(scale, _globalSpeedScale)) return;
        _globalSpeedScale = scale;
        OnGlobalSpeedScaleChanged?.Invoke(_globalSpeedScale);
    }

    //Speed Modifiers
    public static float MINIMUM_SPEED_MULTIPLIER = 0.25f;
    private static readonly Dictionary<string, float> _globalModDefs = new Dictionary<string, float>
    {
        { "mud",   0.40f },
        { "goat",  0.50f },
        { "lightning", 2f },
        { "tailwind",  1.75f },
        { "holdhorse", 0.35f },
        { "chase" , 1.5f }
    };
    private static readonly HashSet<string> _enabledGlobalMods = new HashSet<string>();
    private readonly HashSet<string> _enabledMods = new HashSet<string>();


    public static void RegisterGlobalSpeedModifier(string key, float multiplier)
    {
        _globalModDefs[key] = Mathf.Max(MINIMUM_SPEED_MULTIPLIER, multiplier);
    }
    public static bool TryGetGlobalSpeedModifier(string key, out float mult)
        => _globalModDefs.TryGetValue(key, out mult);

    public void ModifySpeed(string key, float fallbackIfUndefined = 1f)
    {
        if (!_globalModDefs.ContainsKey(key))
        {
            if (fallbackIfUndefined > 0f)
                _globalModDefs[key] = Mathf.Max(MINIMUM_SPEED_MULTIPLIER, fallbackIfUndefined);
            else
                return;
        }
        if (_enabledMods.Add(key)) RecomputeAndApplyEffectiveSpeed();
    }
    public static void EnableSpeedModifier(string key, float fallbackIfUndefined = 1f)
    {
        if (!_globalModDefs.ContainsKey(key))
        {
            if (fallbackIfUndefined > 0f)
                _globalModDefs[key] = Mathf.Max(MINIMUM_SPEED_MULTIPLIER, fallbackIfUndefined);
            else
                return;
        }
        if (_enabledGlobalMods.Add(key))
            OnAnyGlobalSpeedFactorChanged?.Invoke();
    }

    public static void DisableSpeedModifier(string key)
    {
        if (_enabledGlobalMods.Remove(key))
            OnAnyGlobalSpeedFactorChanged?.Invoke();
    }

    public void RevertSpeed(string key)
    {
        if (_enabledMods.Remove(key)) RecomputeAndApplyEffectiveSpeed();
    }

    public void DisableAllSpeedModifiers()
    {
        if (_enabledMods.Count == 0) return;
        _enabledMods.Clear();
        RecomputeAndApplyEffectiveSpeed();
    }

    protected float _effectiveSpeedScale = 1f;

    protected float ComputeEffectiveSpeedScale()
    {
        float s = GlobalSpeedScale;

        foreach (var key in _enabledGlobalMods)
            s *= _globalModDefs[key];

        foreach (var key in _enabledMods)
            s *= _globalModDefs[key];

        return Mathf.Max(MINIMUM_SPEED_MULTIPLIER, s);
    }

    protected void RecomputeAndApplyEffectiveSpeed()
    {
        _effectiveSpeedScale = ComputeEffectiveSpeedScale();
        ApplyEffectiveSpeedScale(_effectiveSpeedScale);
    }

    protected virtual void ApplyEffectiveSpeedScale(float scale)
    {
        currentSpeed = speed * scale;
    }

    private void HandleGlobalSpeedScaleChanged(float _) => RecomputeAndApplyEffectiveSpeed();
    private void HandleAnyGlobalSpeedFactorChanged() => RecomputeAndApplyEffectiveSpeed();

    protected virtual void OnEnable()
    {
        OnGlobalSpeedScaleChanged += HandleGlobalSpeedScaleChanged;
        OnAnyGlobalSpeedFactorChanged += HandleAnyGlobalSpeedFactorChanged;
        RecomputeAndApplyEffectiveSpeed();
    }
    protected virtual void OnDisable()
    {
        OnGlobalSpeedScaleChanged -= HandleGlobalSpeedScaleChanged;
        OnAnyGlobalSpeedFactorChanged -= HandleAnyGlobalSpeedFactorChanged;
    }

    protected virtual void Awake()
    {
        SetVerticalLimits(GameController.gameManager.playArea);
    }

    public virtual void Start()
    {
        levelManager = GameController.animalLevelManager; 
        gameManager = GameController.gameManager;
        player = GameController.player;
        //level = levelManager.GetLevel(name);
        try
        {
            if (GameController.boonManager.ContainsBoon(animalData.legendaryBoon.name))
            {
                legendary = true;
                ActivateLegendary();
            }
        }
        catch (Exception e)
        { //
        }
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 rightEdge = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2f, cam.nearClipPlane));
            Vector3 leftEdge = cam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2f, cam.nearClipPlane));
            leftEdgeX = leftEdge.x - 1f;

            // Set starting position slightly offscreen right
            startPos = new Vector3(rightEdge.x + 1f, transform.position.y, transform.position.z);
            transform.position = startPos;
        }
        SubscribeToBeeStingEvents();
    }
    

    public void Move()
    {
        ApplyRepelFromNearbyAnimals();

        overriddenByAttraction = false;

        Vector3 nextPos;
        if ((GameController.gameManager != null && GameController.gameManager.roundCompleted) || forceExit)
        {
            nextPos = LeaveScreen();
        }
        else if (TryPredatorAttractionOverride(out nextPos))
        {
            overriddenByAttraction = true; // for custom predator tilts to fall back to base tilt
        }
        else
        {
            nextPos = ComputeMove();
        }

        nextPos += externalOffset;

        if (ShouldClampY)
        {
            nextPos.y = ClampY(nextPos.y);
        }

        transform.position = nextPos;
        externalOffset = Vector3.zero;

        if (nextPos.x < leftEdgeX - 5)
        {
            Destroy(gameObject);
        }
    }



    public virtual void ApplyExternalOffset(Vector3 offset)
    {
        // Instead of applying immediately, find the strongest offset
        if (offset.magnitude > pendingExternalOffset.magnitude)
            pendingExternalOffset = offset;
    }

    protected virtual Vector3 ComputeMove()
    {
        // Default behavior: move left across screen
        return transform.position + Vector3.left * currentSpeed * Time.deltaTime;
    }

    public virtual Vector3 LeaveScreen()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        float leaveSpeed = 5f;
        if (SceneManager.GetActiveScene().name == "TitleScreen")
        {
            leaveSpeed = 16f;
        }
        return transform.position + Vector3.left * leaveSpeed * Time.deltaTime;
    }

    public void Update()
    {
        if (!isLassoed)
        {
            externalOffset = pendingExternalOffset;
            pendingExternalOffset = Vector3.zero;
            Move();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!isLassoed)
        {
            actualSpeed = (transform.position - previousPosition).magnitude / Time.deltaTime;
            previousPosition = transform.position;
            ApplyRunTilt();
        }
    }

    public void SetVerticalLimits(RectTransform rect, Camera worldCam = null)
    {
        // Resolve camera: prefer the canvas camera if this rect is on a non-overlay canvas
        if (!worldCam)
        {
            if (rect)
            {
                var root = rect.GetComponentInParent<Canvas>()?.rootCanvas;
                if (root && root.renderMode != RenderMode.ScreenSpaceOverlay && root.worldCamera)
                    worldCam = root.worldCamera;
            }
            if (!worldCam) worldCam = Camera.main;
        }
        if (!worldCam) return;

        // Distance from camera to this object along camera forward (works for ortho and perspective)
        float zDist = Mathf.Abs(Vector3.Dot(transform.position - worldCam.transform.position, worldCam.transform.forward));

        // Sprite half-height so we keep the whole sprite inside
        float halfHeight = 0f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr) halfHeight = sr.bounds.extents.y;

        if (rect)
        {
            // Convert rect corners to screen pixels
            Vector3[] wc = new Vector3[4];
            rect.GetWorldCorners(wc);

            var root = rect.GetComponentInParent<Canvas>()?.rootCanvas;
            bool overlay = root && root.renderMode == RenderMode.ScreenSpaceOverlay;

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(overlay ? null : worldCam, wc[i]);
                if (sp.x < minX) minX = sp.x;
                if (sp.x > maxX) maxX = sp.x;
                if (sp.y < minY) minY = sp.y;
                if (sp.y > maxY) maxY = sp.y;
            }

            // Clamp to the camera's visible pixel rect so offscreen UI does not skew results
            Rect pr = worldCam.pixelRect;
            minY = Mathf.Max(minY, pr.yMin);
            maxY = Mathf.Min(maxY, pr.yMax);
            float centerX = Mathf.Clamp(0.5f * (minX + maxX), pr.xMin, pr.xMax);

            // If the rect is completely outside the camera view, fall back to camera bounds
            if (minY > maxY)
            {
                Vector3 topV = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDist));
                Vector3 botV = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDist));
                topLimitY = topV.y - halfHeight;
                bottomLimitY = botV.y + halfHeight;
                return;
            }

            // Project those screen pixels back to the object plane
            Vector3 topWorld = worldCam.ScreenToWorldPoint(new Vector3(centerX, maxY, zDist));
            Vector3 botWorld = worldCam.ScreenToWorldPoint(new Vector3(centerX, minY, zDist));

            topLimitY = topWorld.y - halfHeight;
            bottomLimitY = botWorld.y + halfHeight;
        }
        else
        {
            // No rect provided: use full camera viewport (respects letterbox/pillarbox)
            Vector3 topWorld = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 1f, zDist));
            Vector3 botWorld = worldCam.ViewportToWorldPoint(new Vector3(0.5f, 0f, zDist));
            topLimitY = topWorld.y - halfHeight;
            bottomLimitY = botWorld.y + halfHeight;
        }
    }


    protected float ClampY(float y)
    {
        return Mathf.Clamp(y, bottomLimitY, topLimitY);
    }

    protected virtual void ApplyRunTilt()
    {
        // Advance the tilt cycle
        tiltProgress += Time.deltaTime * tiltFrequency;

        // Use a safe denominator; if speed is ~0, treat as 1 to avoid divide-by-zero
        float denom = Mathf.Max(0.0001f, Mathf.Abs(speed));
        float speedFactor = Mathf.Clamp01(actualSpeed / denom);

        float amplitude = maxTiltAmplitude * speedFactor;
        float angle = Mathf.Sin(tiltProgress * Mathf.PI * 2f) * amplitude;

        if (float.IsNaN(angle) || float.IsInfinity(angle)) angle = 0f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }


    private void ApplyRepelFromNearbyAnimals()
    {
        if (IsRepelImmune) return; // Skip if animal is immune

        Animal[] allAnimals = FindObjectsOfType<Animal>();
        foreach (var other in allAnimals)
        {
            if (other == this || other.isLassoed)
                continue;

            Vector3 toOther = other.transform.position - transform.position;
            float dist = toOther.magnitude;

            if (dist > 0f && dist < minimumSpacing)
            {
                Vector3 pushDir = -toOther.normalized;
                float strength = (minimumSpacing - dist) / minimumSpacing;
                Vector3 push = pushDir * strength * repelForce * Time.deltaTime;
                ApplyExternalOffset(push);
            }
        }
    }


    protected bool overriddenByAttraction = false;
    protected GameObject attractTarget = null;
    public GameObject AttractTarget => attractTarget;
    [SerializeField] protected bool leftIsPositiveScaleX = true;

    public float attractBrakeRadius = 2.0f;  // start slowing down inside this radius
    public float attractArriveRadius = 0.25f; // consider 'arrived' inside this radius
    public float attractSmoothTime = 0.25f; // SmoothDamp time (you already have)
    public float attractStopThreshold = 0.02f; // consider stopped below this speed
    private float _attractSpeedVel = 0f;

    public bool SetAttractTarget(GameObject a, bool force = false)
    {
        if (!force && attractTarget != null && attractTarget.activeInHierarchy)
            return false;

        attractTarget = a;
        _attractSpeedVel = 0f; 
        return true;
    }

    public void ClearAttractTarget()
    {
        attractTarget = null;
    }

    protected virtual void FaceByX(float xDir)
    {
        // Default: positive scale.x = facing LEFT
        var sc = transform.localScale;
        float abs = Mathf.Abs(sc.x);
        if (leftIsPositiveScaleX)
            sc.x = (xDir >= 0f) ? -abs : abs;   
        else
            sc.x = (xDir >= 0f) ? abs : -abs; 
        transform.localScale = sc;
    }
    protected virtual bool TryPredatorAttractionOverride(out Vector3 nextPos)
    {
        nextPos = transform.position;

        if (!isPredator) return false;
        if (attractTarget == null) return false;

        if (!attractTarget.gameObject.activeInHierarchy)
        {
            attractTarget = null;
            return false;
        }

        Vector3 to = attractTarget.transform.position - transform.position;
        float dist = to.magnitude;

        // --- Compute a *ramped* target speed ---
        //  dist >= brakeRadius         -> full speed
        //  arriveRadius < dist < brake -> scaled speed
        //  dist <= arriveRadius        -> 0 (arrived)
        float t = 1f; // scaling 0..1
        if (dist <= attractArriveRadius)
            t = 0f;
        else if (dist < attractBrakeRadius)
            t = Mathf.InverseLerp(attractArriveRadius, attractBrakeRadius, dist); // 0..1

        float speedTarget = speed * t;

        // Smoothly approach speedTarget
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speedTarget, ref _attractSpeedVel, attractSmoothTime);

        // Face horizontally toward the target
        if (Mathf.Abs(to.x) > 0.001f) FaceByX(to.x);

        // Move toward the target (with overshoot clamp)
        if (dist > 0.000001f && currentSpeed > 0f)
        {
            Vector3 dir = to / dist;
            float step = currentSpeed * Time.deltaTime;
            if (step > dist) step = dist;
            nextPos = transform.position + dir * step;
        }

        // If firmly inside arrive radius and basically stopped, hold position
        if (dist <= attractArriveRadius && currentSpeed <= attractStopThreshold)
            nextPos = transform.position;

        return true; // attraction owns movement this frame
    }

    public void StruckByLightning()
    {
        ModifySpeed("lightning");
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.material = GameController.lightningMat;
        struckByLightning = true;
        DOTween.Sequence()
            .Append(sr.material.DOFloat(0.35f, "_FlashAmount", 0.5f))
            .Append(sr.material.DOFloat(0, "_FlashAmount", 0.5f)).SetLoops(-1);
    }



    public virtual void ActivateLegendary()
    {
        
    }

    private void SubscribeToBeeStingEvents()
    {
        Bee.OnAnyBeeSting += OnBeeStingDetected;
    }

    private void UnsubscribeFromBeeStingEvents()
    {
        Bee.OnAnyBeeSting -= OnBeeStingDetected;
    }

    private void OnBeeStingDetected(Animal stungAnimal)
    {
        if (stungAnimal == this)
        {
            DieFromBee();
        }
    }

    void DieFromBee()
    {
        isLassoed = true;
        currentSpeed = 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        
        DOTween.Sequence()
            .Append(transform.DOMoveY(transform.position.y-.5f, 0.5f).SetEase(Ease.InBack))
            .Join(transform.DOScaleY(-1, 0f))
            .Join(sr.DOFade(0f, 0.5f))
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        UnsubscribeFromBeeStingEvents();
    }

}