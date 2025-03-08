using Autodesk.Fbx;
using StarterAssets;
using System.Collections;
using System.Threading;
using Unity.Cinemachine;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.Rendering;// Kiểm tra xem hệ thống Input System có được bật không, nếu có thì import
#endif

[RequireComponent(typeof(CharacterController))] //Yêu cầu GameObject phải có CharacterController.
[RequireComponent(typeof(PlayerInput))] //Yêu cầu GameObject phải có PlayerInput.
public class PlayerMovemnet : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed;// tốc độ di chuyển
    [SerializeField]
    private float sprintSpeed;// chạy
    [Range(0f, 0.3f)] // khoảng xoay nhân vật theo hướng di chuyển
    private float rotationSmoothTime = 0.12f;// làm muọt xoay nhân vật theo thời gian.
    [SerializeField]
    private float speedChangeRate;// chỉnh tốc đọ di chuyển.
    [SerializeField]
    private float jumpHeight;// chiều cao nhảy
    [SerializeField]
    private float jumpTimeOut;// thời gian chờ để nhảy tiếp
    [SerializeField]
    private float fallTimeOut;// thời gian để kiểm tra nhân vật đang rơi
    [SerializeField]
    private float gravity;// trọng lực
    [SerializeField]
    private bool Grounded;// kiểm tra trạng thái tiếp đất.
    [SerializeField]
    private float groundedOffset;// kiểm tra khoảng cách với mặt đất
    [SerializeField]
    private float groundedRadius;// bán kính của hình cầu để kiểm tra chạm đất
    [SerializeField]
    private LayerMask GroundedLayers;//kiểm tra những obj có layer là ground.
    [SerializeField]
    private GameObject CinemachineCameraTarget; // obj cần target camera
    [SerializeField]
    private float topClamp;//giới hạn quay lên
    [SerializeField]
    private float bottomClamp;// giới hạn quay xuống
    [SerializeField]
    private float CameraAngleOverride;// chỉnh góc cam  aim_default vào nhân vật.
    [SerializeField]
    private bool lockCameraPosition=false;// lock cam ko cho xoay.


    //các biến mặc định của nhân vật

    // cinemachine
    private float _cinemachineTargetYaw;// quay trái phải
    private float _cinemachineTargetPitch;// quay lên xuống

    // player
    private float _speed;// tốc độ di chuyển mặc định
    private float _animationBlend;// chỉnh độ mượt của animation khi đang di chuyển
    private float _targetRotation = 0.0f;//góc xoay nhân vật theo hướng di chuyển.
    private float _rotationVelocity;// tốc độ xoay
    private float _verticalVelocity;// tốc độ nhảy và rơi.
    private float _terminalVelocity = 53.0f; // giới hạn tốc độ rơi.

    // timeout deltatime
    private float _jumpTimeoutDelta;// kiểm soát tránh spam nhảy lấy biến - time.deltatime >=0 mới cho nảy tiếp.
    private float _fallTimeoutDelta;// thời gian để chuyển sang trang thái rơi. (khi bước xuống cầu thang trong thật nhất)

    // animation IDs
    private int _animIDSpeed;// phát hiện trạng thái chạy và cập nhật giá trị animation theo.
    private int _animIDGrounded;// kiểm tra play trạng thái đứng yên và nhảy/rơi.
    private int _animIDJump;// play animation nhảy và idle.
    private int _animIDFreeFall;// play trạng thái rơi và tiếp đất.
    private int _animIDMotionSpeed;// điều chỉnh tốc độ của animation theo di chuyển của player.

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private CharacterController _controller;
    private StarterAssetsInputs _input;//Dùng để nhận thông tin điều khiển từ người chơi (bàn phím, tay cầm, chuột).
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;//Dùng để bỏ qua những chuyển động rất nhỏ, tránh lỗi rung.

    private bool _hasAnimator;// kiểm tra có animator không.

    private bool IsCurrentDeviceMouse // kiểm tra phải đang dùng phím và chuột
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";// điều kiện true đang dùng.
#else
				return false;
#endif
        }
    }


    private void Awake()
    {
        if(_mainCamera == null)// check null cam
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");// tìm obj có tag "main camera".
        }

    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;// giá trị xoay quanh trục y.

#if ENABLE_INPUT_SYSTEM
        _playerInput=GetComponent<PlayerInput>();
#endif
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();
        _hasAnimator= TryGetComponent(out  _animator);

        AssignAnimationIDs();
        // reset our timeouts on start
        _jumpTimeoutDelta = jumpTimeOut;
        _fallTimeoutDelta = fallTimeOut;
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);
        GroundedCheck();


    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");          // chuyển đổi kiểu string thành int tối ưu hoá hơn.
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck() // kiểm tra va chạm mặt đất
    {
        Vector3 SpherePosion = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);// lấy vị trí theo player
                                    // (transform.position.y-groundedoffset kiểm tra trên bề mặt gồ ghề luôn nằm dưới chân)

        Grounded = Physics.CheckSphere(SpherePosion,groundedRadius,GroundedLayers,QueryTriggerInteraction.Ignore);// tạo hình cầu check
                                   // querytriggeraction.ignore không kiểm tra bỏ qua các collider trigger.

        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded); // bật trạng thái chạm đất.
        }
    }

    private void CameraRotation() // xoay cam
    {
        if(_input.look.sqrMagnitude >= _threshold && !lockCameraPosition) 
            // bình phương hướng di chuyển của cam lớn hơn bằng threshold tránh lỗi rung khi input quá nhỏ.
           // và kiểm tra cam không bị lock.
        {
            // chỉnh tốc độ camera theo bộ điều khiển chuột(1.0f) và tay cầm(time.deltatime).
            // dùng chuột giá trị giữ nguyên nền = 1f.0f còn dùng tay cầm cần độ mượt nên dùng deltatime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.look.y * deltaTimeMultiplier;// cập nhật góc xoay theo trục x xoay ngang
            _cinemachineTargetPitch += _input.look.x * deltaTimeMultiplier;// cập nhật góc xoay theo trục y xoay dọc

            // giới hạng góc xoay min và max
            _cinemachineTargetYaw = Mathf.Clamp(deltaTimeMultiplier, float.MinValue, float.MaxValue);// từ -360 đến 360
            _cinemachineTargetPitch = Mathf.Clamp(deltaTimeMultiplier, bottomClamp, topClamp);// giới hạn bottomclamp qua topclamp

            //cập nhật camera vào obj target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetYaw * CameraAngleOverride, _cinemachineTargetPitch, 0f);
        }
    }

    private void Move()
    {
        // nhận biết input chạy và đi bộ
        float targetspeed = _input.sprint ? sprintSpeed : moveSpeed;

        if (_input.move == Vector2.zero) targetspeed = 0.0f;// không có input vào set tốc độ =0.0f để giúp nhân vật dừng tự nhiên.

        float currenHorizontalSpeed = new Vector3(_controller.velocity.x,0.0f,_controller.velocity.z).magnitude;
        // cập nhật value vào X(trái/phải) và Z(tiến/lùi) chỉ khi player trên mặt đất dùng magnitube để cập nhật value vào velocity.
        float speedOfset = 0.1f; // tránh giật lag.
        float inputMagnitube =_input.analogMovement ? _input.move.magnitude : 1.0f ;
        // điều kiện true láy value cập nhật vào bộ điều khiển tay cầm còn lại lấy 1.0f vào keyboardmouse.

        // không làm thay đổi value ngay lập tức mất tự nhiên
        if (currenHorizontalSpeed < targetspeed + speedOfset || currenHorizontalSpeed > targetspeed - speedOfset)
        // nếu tốc độ nhỏ hơn targetspeed thì cho tiếp tục tăng và nếu tốc độ lớn hơn thì giảm lại đến giá trị gần bé hơn bằng target.
        {

        }
    }



}




