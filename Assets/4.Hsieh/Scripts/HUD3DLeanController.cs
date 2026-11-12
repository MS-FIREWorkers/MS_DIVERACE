
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class HUD3DLeanController : MonoBehaviour
{
    // ==========================================
    // ----- HUD References -----
    // ==========================================

    [Header("HUDの設定")]

    [FormerlySerializedAs("visorRoot")]
    [InspectorName("動くHUD（回転・縮小）")]
    [Tooltip("プレイヤーの加速度で傾き、高速落下時に縮小するHUD")]
    [SerializeField]
    private RectTransform rotatingHUD;

    [FormerlySerializedAs("hudRoot")]
    [InspectorName("固定情報HUD（動かない）")]
    [Tooltip("HPや速度などの情報HUD。このスクリプトでは動かさない")]
    [SerializeField]
    private RectTransform staticInfoHUD;

    // プレイヤー関連コンポーネントは自動取得
    // Inspectorには表示しない
    private Player_Movement playerMovement;
    private Player_InputHandler playerInput;

    // Inspectorの取得状況表示用
    public bool HasAutoPlayerReferences =>
        playerMovement != null && playerInput != null;

    // ==========================================
    // ----- Acceleration Settings -----
    // ==========================================

    [Header("HUDの揺れ：反応設定")]

    [InspectorName("左右の揺れやすさ")]
    [Tooltip("小さいほど左右に大きく傾きやすい")]
    [SerializeField, Min(0.1f)]
    private float sideAccelerationForMaxTilt = 15f;

    [InspectorName("前後の揺れやすさ")]
    [Tooltip("小さいほど前後に大きく傾きやすい")]
    [SerializeField, Min(0.1f)]
    private float forwardAccelerationForMaxTilt = 20f;

    [InspectorName("小さな揺れを無視")]
    [Tooltip("この値より小さな加速度を無視する")]
    [SerializeField, Min(0f)]
    private float accelerationDeadZone = 0.5f;

    [InspectorName("揺れの反応速度")]
    [Tooltip("大きいほど加速度の変化に素早く反応する")]
    [SerializeField, Min(0.1f)]
    private float accelerationFilterSpeed = 12f;

    // ==========================================
    // ----- 3D Tilt Settings -----
    // ==========================================

    [Header("HUDの揺れ：角度設定")]

    [InspectorName("左右の最大角度")]
    [Tooltip("左右移動による最大回転角度")]
    [SerializeField, Range(0f, 30f)]
    private float horizontalTiltAngle = 8f;

    [InspectorName("前後の最大角度")]
    [Tooltip("前後移動による最大回転角度")]
    [SerializeField, Range(0f, 30f)]
    private float verticalTiltAngle = 5f;

    // ==========================================
    // ----- Tilt Smooth Settings -----
    // ==========================================

    [Header("HUDの揺れ：なめらかさ")]

    [InspectorName("揺れる時の時間")]
    [Tooltip("傾きが大きくなる時の補間時間")]
    [SerializeField, Min(0.01f)]
    private float tiltSmoothTime = 0.16f;

    [InspectorName("戻る時の時間")]
    [Tooltip("傾きが小さくなる時の補間時間")]
    [SerializeField, Min(0.01f)]
    private float returnSmoothTime = 0.25f;

    // ==========================================
    // ----- Vertical Fall Scale Settings -----
    // ==========================================

    [Header("高速落下時のHUD縮小")]

    [InspectorName("縦姿勢の判定値")]
    [Tooltip("この姿勢入力値を超えると縮小を開始できる")]
    [SerializeField, Range(0f, 1f)]
    private float verticalPostureThreshold = 0.75f;

    [InspectorName("縮小開始速度")]
    [Tooltip("HUDが縮小し始める落下速度")]
    [SerializeField, Min(0f)]
    private float shrinkStartFallSpeed = 20f;

    [InspectorName("最大縮小速度")]
    [Tooltip("HUDが最小サイズになる落下速度")]
    [SerializeField, Min(0f)]
    private float shrinkMaxFallSpeed = 45f;

    [InspectorName("最小サイズ")]
    [Tooltip("0.97なら元の97%のサイズ")]
    [SerializeField, Range(0.8f, 1f)]
    private float verticalFallScale = 0.97f;

    [InspectorName("縮小する時間")]
    [Tooltip("HUDが縮小する時の補間時間")]
    [SerializeField, Min(0.01f)]
    private float shrinkSmoothTime = 0.20f;

    [InspectorName("元に戻る時間")]
    [Tooltip("通常サイズへ戻る時の補間時間")]
    [SerializeField, Min(0.01f)]
    private float scaleReturnSmoothTime = 0.25f;

    // ==========================================
    // ----- Acceleration Debug -----
    // ==========================================

    [Header("テスト：HUDの揺れ")]

    [InspectorName("手動テストを使う")]
    [Tooltip("ONで下のテスト方向を使用する")]
    [SerializeField]
    private bool debugOverride = false;

    [InspectorName("テスト方向（X:左右 / Y:前後）")]
    [SerializeField]
    private Vector2 debugAcceleration;

    [InspectorName("実際の加速度（確認用）")]
    [SerializeField]
    private Vector2 debugActualAcceleration;

    [InspectorName("現在の揺れ（確認用）")]
    [SerializeField]
    private Vector2 debugCurrentTilt;

    // ==========================================
    // ----- Vertical Fall Debug -----
    // ==========================================

    [Header("テスト：高速落下時の縮小")]

    [InspectorName("手動テストを使う")]
    [SerializeField]
    private bool debugFallOverride = false;

    [InspectorName("テスト姿勢（0:水平 / 1:縦）")]
    [SerializeField, Range(0f, 1f)]
    private float debugPoseInput = 1f;

    [InspectorName("テスト落下速度")]
    [SerializeField, Min(0f)]
    private float debugFallSpeed = 40f;

    [InspectorName("現在の姿勢（確認用）")]
    [SerializeField]
    private float currentPosture;

    [InspectorName("現在の落下速度（確認用）")]
    [SerializeField]
    private float currentFallSpeed;

    [InspectorName("現在のHUDサイズ（確認用）")]
    [SerializeField]
    private float currentHUDScale = 1f;

    // ==========================================
    // ----- Runtime -----
    // ==========================================

    private Quaternion initialRotatingHUDRotation;
    private Vector3 initialRotatingHUDScale;

    private Vector3 previousHorizontalVelocity;
    private bool hasPreviousVelocity;

    private Vector2 filteredAcceleration;
    private Vector2 currentTilt;
    private Vector2 tiltVelocity;

    private float currentScale = 1f;
    private float scaleVelocity;

    private float nextSearchTime;
    private bool initialized;

    // ==========================================
    // ----- Unity Events -----
    // ==========================================

    private void Awake()
    {
        if (rotatingHUD == null)
        {
            Debug.LogError(
                "HUD3DLeanController: Rotating HUD is missing.",
                this
            );

            enabled = false;
            return;
        }

        initialRotatingHUDRotation = rotatingHUD.localRotation;
        initialRotatingHUDScale = rotatingHUD.localScale;

        ValidateHUDHierarchy();

        // プレイヤーを自動取得
        FindPlayerReferences();

        initialized = true;
    }

    private void OnEnable()
    {
        hasPreviousVelocity = false;
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        // プレイヤーが取得できなかった場合は再検索
        if ((playerMovement == null || playerInput == null) &&
            Time.unscaledTime >= nextSearchTime)
        {
            FindPlayerReferences();
            nextSearchTime = Time.unscaledTime + 1f;
        }

        UpdateInertiaTilt(dt);
        UpdateVerticalFallScale(dt);
    }

    // ==========================================
    // ----- Validate HUD Hierarchy -----
    // ==========================================

    private void ValidateHUDHierarchy()
    {
        if (staticInfoHUD == null)
            return;

        if (staticInfoHUD == rotatingHUD ||
            staticInfoHUD.IsChildOf(rotatingHUD) ||
            rotatingHUD.IsChildOf(staticInfoHUD))
        {
            Debug.LogWarning(
                "Rotating HUD and Static Info HUD " +
                "should be separate sibling objects.",
                this
            );
        }

        Canvas rotatingCanvas =
            rotatingHUD.GetComponentInParent<Canvas>();

        Canvas staticCanvas =
            staticInfoHUD.GetComponentInParent<Canvas>();

        if (rotatingCanvas == null ||
            staticCanvas == null ||
            rotatingCanvas.rootCanvas != staticCanvas.rootCanvas)
        {
            Debug.LogWarning(
                "Both HUD objects should belong " +
                "to the same Canvas hierarchy.",
                this
            );
        }
    }

    // ==========================================
    // ----- Inertia Tilt -----
    // ==========================================

    private void UpdateInertiaTilt(float dt)
    {
        Vector2 acceleration =
            CalculateNormalizedAcceleration(dt);

        debugActualAcceleration = acceleration;

        if (debugOverride)
        {
            acceleration = Vector2.ClampMagnitude(
                debugAcceleration,
                1f
            );
        }

        // 加速度の変化を平滑化
        float filterFactor =
            1f - Mathf.Exp(-accelerationFilterSpeed * dt);

        filteredAcceleration = Vector2.Lerp(
            filteredAcceleration,
            acceleration,
            filterFactor
        );

        // 加速度と逆方向へ傾ける
        Vector2 targetTilt = -filteredAcceleration;

        float smoothTime =
            targetTilt.sqrMagnitude >
            currentTilt.sqrMagnitude
            ? tiltSmoothTime
            : returnSmoothTime;

        currentTilt = Vector2.SmoothDamp(
            currentTilt,
            targetTilt,
            ref tiltVelocity,
            smoothTime,
            Mathf.Infinity,
            dt
        );

        currentTilt = Vector2.ClampMagnitude(
            currentTilt,
            1f
        );

        debugCurrentTilt = currentTilt;

        // 3D回転
        float rotationX =
            -currentTilt.y * verticalTiltAngle;

        float rotationY =
            currentTilt.x * horizontalTiltAngle;

        rotatingHUD.localRotation =
            initialRotatingHUDRotation *
            Quaternion.Euler(rotationX, rotationY, 0f);
    }

    // ==========================================
    // ----- Calculate Acceleration -----
    // ==========================================

    private Vector2 CalculateNormalizedAcceleration(float dt)
    {
        if (playerMovement == null)
        {
            hasPreviousVelocity = false;
            return Vector2.zero;
        }

        Vector3 velocity = playerMovement.CurrentVelocity;

        // 落下速度を除外
        velocity.y = 0f;

        // 初回は速度だけ保存
        if (!hasPreviousVelocity)
        {
            previousHorizontalVelocity = velocity;
            hasPreviousVelocity = true;
            return Vector2.zero;
        }

        Vector3 acceleration =
            (velocity - previousHorizontalVelocity) / dt;

        previousHorizontalVelocity = velocity;

        Transform player = playerMovement.transform;

        Vector3 right = player.right;
        Vector3 forward = player.forward;

        right.y = 0f;
        forward.y = 0f;

        if (right.sqrMagnitude < 0.0001f ||
            forward.sqrMagnitude < 0.0001f)
        {
            return Vector2.zero;
        }

        right.Normalize();
        forward.Normalize();

        float side = Vector3.Dot(acceleration, right);
        float front = Vector3.Dot(acceleration, forward);

        // 小さい加速度を無視
        if (Mathf.Abs(side) < accelerationDeadZone)
            side = 0f;

        if (Mathf.Abs(front) < accelerationDeadZone)
            front = 0f;

        Vector2 normalized = new Vector2(
            side / Mathf.Max(0.1f, sideAccelerationForMaxTilt),
            front / Mathf.Max(0.1f, forwardAccelerationForMaxTilt)
        );

        return Vector2.ClampMagnitude(normalized, 1f);
    }

    // ==========================================
    // ----- Vertical Fall Scale -----
    // ==========================================

    private void UpdateVerticalFallScale(float dt)
    {
        float posture = 0f;
        float fallSpeed = 0f;

        if (debugFallOverride)
        {
            posture = Mathf.Clamp01(debugPoseInput);
            fallSpeed = Mathf.Max(0f, debugFallSpeed);
        }
        else
        {
            if (playerInput != null)
            {
                posture = Mathf.Clamp01(
                    playerInput.PoseInput
                );
            }

            if (playerMovement != null)
            {
                fallSpeed = Mathf.Max(
                    0f,
                    -playerMovement.CurrentVelocity.y
                );
            }
        }

        currentPosture = posture;
        currentFallSpeed = fallSpeed;

        // 縦姿勢の割合
        float postureFactor = Mathf.InverseLerp(
            verticalPostureThreshold,
            1f,
            posture
        );

        // 落下速度の割合
        float maxSpeed = Mathf.Max(
            shrinkStartFallSpeed + 0.01f,
            shrinkMaxFallSpeed
        );

        float speedFactor = Mathf.InverseLerp(
            shrinkStartFallSpeed,
            maxSpeed,
            fallSpeed
        );

        // 縦姿勢かつ高速落下時のみ縮小
        float shrinkFactor = postureFactor * speedFactor;

        float targetScale = Mathf.Lerp(
            1f,
            verticalFallScale,
            shrinkFactor
        );

        // 縮小・復帰の補間
        float smoothTime =
            targetScale < currentScale
            ? shrinkSmoothTime
            : scaleReturnSmoothTime;

        currentScale = Mathf.SmoothDamp(
            currentScale,
            targetScale,
            ref scaleVelocity,
            smoothTime,
            Mathf.Infinity,
            dt
        );

        rotatingHUD.localScale =
            initialRotatingHUDScale * currentScale;

        currentHUDScale = currentScale;
    }

    // ==========================================
    // ----- Auto Find Player -----
    // ==========================================

    private void FindPlayerReferences()
    {
        Player_Movement previousPlayer = playerMovement;

        // シーン内のPlayer_Movementを自動取得
        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<Player_Movement>();
        }

        if (playerMovement != previousPlayer)
        {
            playerInput = null;
            hasPreviousVelocity = false;
        }

        // 同じプレイヤーのInputHandlerを取得
        if (playerInput == null && playerMovement != null)
        {
            playerInput =
                playerMovement.GetComponent<Player_InputHandler>();
        }
    }

    // ==========================================
    // ----- Reset -----
    // ==========================================

    private void OnDisable()
    {
        if (!initialized)
            return;

        if (rotatingHUD != null)
        {
            rotatingHUD.localRotation =
                initialRotatingHUDRotation;

            rotatingHUD.localScale =
                initialRotatingHUDScale;
        }

        filteredAcceleration = Vector2.zero;
        currentTilt = Vector2.zero;
        tiltVelocity = Vector2.zero;

        currentScale = 1f;
        scaleVelocity = 0f;

        currentHUDScale = 1f;
        hasPreviousVelocity = false;
    }
}
