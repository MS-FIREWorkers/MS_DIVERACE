/*
    Player_Animation
    20261008  hanaue sho
 */
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Player_Animation : MonoBehaviour
{
    // --------------------------------------------------
    // ----- PlayerCharacterBody -----
    // --------------------------------------------------
    [Header("PlayerCharacter Body")]
    [SerializeField] private GameObject _body;

    // --------------------------------------------------
    // ----- Animation Rigging -----
    // --------------------------------------------------
    [Header("Animation Rigging（手動参照）")]
    [SerializeField] private TwoBoneIKConstraint _rightArmIK;
    [SerializeField] private TwoBoneIKConstraint _leftArmIK;

    [Header("Arm IK Blend")]
    [SerializeField, Min(0.0f)] private float _ikBlendSpeed = 5.0f;

    private float _rightIKTargetWeight;
    private float _leftIKTargetWeight;

    // --------------------------------------------------
    // ----- IK Target / Shoulder -----
    // --------------------------------------------------
    [Header("IK Target（手動参照）")]
    [SerializeField] private Transform _rightHandTarget;
    [SerializeField] private Transform _leftHandTarget;

    [Header("IK Shoulder（手動参照）")]
    [SerializeField] private Transform _rightShoulder;
    [SerializeField] private Transform _leftShoulder;

    // --------------------------------------------------
    // ----- CollisionDetector -----
    // --------------------------------------------------
    private Player_CollisionDetector _collisionDetector;

    // --------------------------------------------------
    // ----- Wall IK Parameter -----
    // --------------------------------------------------
    [Header("Wall IK")]
    [SerializeField, Min(0.0f)] private float _ikRayDistance = 3.0f;
    [SerializeField, Min(0.0f)] private float _ikRayOriginOffset = 0.15f;
    [SerializeField] private float _handWallOffset = 0.05f;

    // --------------------------------------------------
    // ----- Wall IK Runtime -----
    // --------------------------------------------------
    private Collider _wallIKCollider;
    private Vector3 _wallIKNormal;
    private bool _useRightWallHand;
    private bool _hasWallIKContact;

    public Vector3 CurrentHandContactPoint { get; private set; }
    public Vector3 CurrentHandContactNormal { get; private set; }
    public bool HasHandContact => _hasWallIKContact;

    // --------------------------------------------------
    // ----- Unity Event -----
    // --------------------------------------------------
    private void Start()
    {
        _collisionDetector = GetComponent<Player_CollisionDetector>();  

        if (_body == null)
        {
            Debug.LogError("[Error] PlayerCharacterBody Not Found!");
        }

        if (_rightArmIK != null)
        {
            _rightArmIK.weight = 0.0f;
        }

        if (_leftArmIK != null)
        {
            _leftArmIK.weight = 0.0f;
        }
    }
    private void Update()
    {
        float dt = Time.deltaTime;

        // ----- Wall IK Target -----
        UpdateWallIKTarget();

        // ----- IK Weight -----
        float rightWeight = _rightIKTargetWeight;
        float leftWeight = _leftIKTargetWeight;

        // 壁面接触を取得できない場合はIKを弱める
        if (_wallIKCollider != null && !_hasWallIKContact)
        {
            rightWeight = 0.0f;
            leftWeight = 0.0f;
        }

        // 
        if (_rightArmIK != null)
        {
            _rightArmIK.weight = Mathf.MoveTowards(_rightArmIK.weight, _rightIKTargetWeight, _ikBlendSpeed * dt);
        }

        if (_leftArmIK != null)
        {
            _leftArmIK.weight = Mathf.MoveTowards(_leftArmIK.weight, _leftIKTargetWeight, _ikBlendSpeed * dt);
        }
    }

    // --------------------------------------------------
    // ----- Update Wall IK Target -----
    // --------------------------------------------------
    private void UpdateWallIKTarget()
    {
        _hasWallIKContact = false;

        if (_wallIKCollider == null || _collisionDetector == null)
        {
            return;
        }

        // 使用する腕
        Transform shoulder = _useRightWallHand ? _rightShoulder : _leftShoulder;
        Transform target = _useRightWallHand ? _rightHandTarget : _leftHandTarget;

        if (shoulder == null || target == null)
        {
            return;
        }

        // ----- Ray Origin -----
        // 壁の外側へ少しずらしてRayを開始
        Vector3 origin = shoulder.position + _wallIKNormal * _ikRayOriginOffset;

        // ----- Raycast -----
        if (!_collisionDetector.TryGetWallHandContact(origin, _wallIKNormal, _wallIKCollider, _ikRayDistance, out RaycastHit hit))
        {
            return;
        }

        // ----- Contact Point -----
        CurrentHandContactPoint = hit.point;
        CurrentHandContactNormal = hit.normal;
        _hasWallIKContact = true;

        // ----- Target Position -----
        Vector3 targetPosition = hit.point + _wallIKNormal * _handWallOffset;

        target.position = targetPosition;
    }


    // --------------------------------------------------
    // ----- Public Event -----
    // --------------------------------------------------
    public void RotateTo(float input)
    {
        input = Mathf.Clamp01(input);

        float rotationX = Mathf.Lerp(90.0f, 180.0f, input);

        Vector3 euler = new Vector3 (rotationX, 0.0f, 0.0f);
        _body.transform.localRotation = Quaternion.Euler(euler);
    }

    // --------------------------------------------------
    // ----- Set Arm IK -----
    // --------------------------------------------------
    public void SetArmIK(bool useRightArm, bool useLeftArm)
    {
        _rightIKTargetWeight = useRightArm ? 1.0f : 0.0f;
        _leftIKTargetWeight = useLeftArm ? 1.0f : 0.0f;
    }

    // --------------------------------------------------
    // ----- Disable Arm IK -----
    // --------------------------------------------------
    public void DisableArmIK()
    {
        _rightIKTargetWeight = 0.0f;
        _leftIKTargetWeight = 0.0f;
    }

    // --------------------------------------------------
    // ----- Set Wall IK Contact -----
    // --------------------------------------------------
    public void SetWallIKContact(Collider wallCollider, Vector3 wallNormal, bool useRightHand)
    {
        _wallIKCollider = wallCollider;
        _wallIKNormal = wallNormal.normalized;
        _useRightWallHand = useRightHand;
        _hasWallIKContact = false;
    }

    // --------------------------------------------------
    // ----- Clear Wall IK Contact -----
    // --------------------------------------------------
    public void ClearWallIKContact()
    {
        _wallIKCollider = null;
        _wallIKNormal = Vector3.zero;
        _hasWallIKContact = false;
    }
}
