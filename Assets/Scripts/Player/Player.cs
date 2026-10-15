using TMPro;
using UnityEngine;
using System.Collections;
using Unity.MLAgents;

public class Player : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] private AudioSource[] _sounds;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _dashSpeed;
    [SerializeField] private float _jumpHeight;
    [SerializeField] private float _maxFallSpeed;
    [SerializeField] private float _airAcceleration;
    [SerializeField] private float _airDrag;
    [SerializeField] private float _dodgeSpeed;
    [SerializeField] private bool _controlling;
    [SerializeField] private PhysicsMaterial2D _bounceMaterial;
    [SerializeField] private TextMeshPro _nameText;
    [SerializeField] private SpriteRenderer[] _limbs;
    [SerializeField] private TrainingAcademy _academy;
    [SerializeField] private float _staminaRegenRate = 0.25f;
    [SerializeField] private Vector3 _staminaBarOffset = new Vector3(0f, 1.8f, 0f);

    public float MovementSpeed => _movementSpeed;
    public float MaxFallSpeed => _maxFallSpeed;
    public int MaxJumps => 2;
    public int MaxDodges => 3;
    public float Stamina => _stamina;

    public float StaminaMultiplier
    {
        get
        {
            if (_stamina >= 0.999f)
            {
                return 1.2f;
            }

            if (_stamina >= 0.799f)
            {
                return 1.0f;
            }

            if (_stamina >= 0.599f)
            {
                return 0.8f;
            }

            if (_stamina >= 0.399f)
            {
                return 0.6f;
            }

            return 0.4f;
        }
    }

    #endregion

    #region Controls

    private InputDevice _device;

    private bool _up, _down, _left, _right;
    private bool _serverJump, _serverDash, _serverAttack, _serverFastFall;

    protected void Jump()
    {
        if (_launched) return;
        _serverJump = true;
    }

    protected void Dash()
    {
        if (_launched) return;
        _serverDash = true;
    }

    protected void FastFall()
    {
        if (_launched) return;
        _serverFastFall = true;
    }

    protected void Attack()
    {
        if (_launched) return;
        _serverAttack = true;
    }

    protected void Up() => _up = true;
    protected void RetractUp() => _up = false;
    protected void Down() => _down = true;
    protected void RetractDown() => _down = false;
    protected void Left() => _left = true;
    protected void RetractLeft() => _left = false;
    protected void Right() => _right = true;
    protected void RetractRight() => _right = false;

    #endregion

    #region Constants

    private const float JumpCooldown = 0.2f;
    private const float DodgeCooldown = 0.1f;
    private const float DashCooldown = 0.25f;
    private const float MinStamina = 0.2f;
    private const float MaxStamina = 1.0f;
    private const float StaminaStep = 0.2f;

    private static readonly int NLightHash = Animator.StringToHash("nlight");
    private static readonly int SLightHash = Animator.StringToHash("slight");
    private static readonly int DLightHash = Animator.StringToHash("dlight");
    private static readonly int NAirHash = Animator.StringToHash("nair");
    private static readonly int SAirHash = Animator.StringToHash("sair");
    private static readonly int DAirHash = Animator.StringToHash("dair");
    private static readonly int DashHash = Animator.StringToHash("dash");
    private static readonly int DodgeHash = Animator.StringToHash("dodge");
    private static readonly int IdleHash = Animator.StringToHash("idle");
    private static readonly int RunHash = Animator.StringToHash("run");

    #endregion

    #region Private Fields

    private string _playerName;
    private Color _color;
    private bool _dashInitiated = false;
    private bool _grounded = true;
    private int _jumps = 2;
    private int _dodges = 3;
    private bool _canJump = true;
    private bool _dodging = false;
    private bool _canRecastDodge = false;
    private float _gravity = 0;
    private bool _jumpInitiated = false;
    private bool _launched = false;
    private bool _attacking = false;
    private float _lastJumpTime = -1f;
    private float _lastDodgeTime = -1f;
    private float _lastDashTime = -1f;
    private float _stamina = 1.0f;
    private StaminaBar _staminaBar;

    private bool CanMove
    {
        get
        {
            var stateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            return stateHash == IdleHash || stateHash == RunHash;
        }
    }

    #endregion

    #region Components

    private Rigidbody2D _rb;
    private Animator _animator;

    #endregion

    #region Animator Methods

    private void PlaySound(int id)
    {
        if (AudioListener.pause || AudioListener.volume <= 0f)
        {
            return;
        }

        _sounds[id].Play();
    }

    private void EnableJump() => _canJump = true;

    private void Respawn()
    {
        if (_academy != null)
        {
            transform.position = _academy.GetRandomSpawnPosition();
        }
        else if (GameManager.Instance != null)
        {
            transform.position = GameManager.Instance.GetRandomSpawnPosition();
        }

        _rb.linearVelocity = Vector2.zero;
    }

    private void ActivateInput()
    {
        _grounded = true;
        _jumps = 2;
        _dodges = 3;
        _jumpInitiated = false;
        _dashInitiated = false;
        StopDodge();
        _launched = false;
        _rb.gravityScale = _gravity;
        _rb.linearVelocity = Vector2.zero;
        _rb.sharedMaterial = null;
        StopAttack();
        transform.right = Vector2.right;
        _canJump = true;
        _stamina = MaxStamina;
        _animator.speed = StaminaMultiplier;
    }

    private void ApplyDashSpeed()
    {
        _rb.linearVelocity = new Vector2((transform.eulerAngles.y > 90 ? -1 : 1) * _dashSpeed * StaminaMultiplier * StaminaMultiplier, _rb.linearVelocity.y);
    }

    private void CancelHorizontalMomentum()
    {
        _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
        _dashInitiated = false;
    }

    private void DodgeDir()
    {
        if (_launched) return;

        Vector2 dir = new Vector2(
            (_right ? 1 : 0) - (_left ? 1 : 0),
            (_up ? 1 : 0) - (_down ? 1 : 0)
        );

        if (_grounded)
        {
            if (dir == Vector2.zero || dir == Vector2.up)
                _rb.linearVelocity = dir * _dodgeSpeed;
        }
        else
        {
            _rb.linearVelocity = dir * _dodgeSpeed;
        }

        _dodging = true;
        _animator.speed = 1f;
        gameObject.layer = LayerMask.NameToLayer("PlayerNoHit");
    }

    private void RecastDodge()
    {
        if (_launched) return;
        _canRecastDodge = true;
        _rb.linearVelocity = Vector2.zero;
    }

    private void StopDodge()
    {
        _dodging = false;
        _animator.speed = StaminaMultiplier;
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void ApplyJumpHeight()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpHeight);
        _dashInitiated = false;
        _jumpInitiated = true;
        _canJump = true;
    }

    private void InitiateAttack()
    {
        _attacking = true;
        _dashInitiated = false;
        _jumpInitiated = false;
    }

    private void ActivateDrag()
    {
        _rb.linearDamping = 4;
    }

    private void StopAttack()
    {
        _attacking = false;
        _rb.linearDamping = 0;
    }

    private void LaunchHorizontal(float speed)
    {
        _rb.linearVelocity = new Vector2((transform.eulerAngles.y > 90 ? -1 : 1) * speed, 0);
    }

    #endregion

    #region Public Methods

    public void Blast()
    {
        _animator.SetTrigger("respawn");
        _rb.linearVelocity = Vector2.zero;
        _rb.gravityScale = 0;
        _launched = true;
    }

    public void Launch(Vector2 dir)
    {
        _academy.OnAgentLose(GetComponent<StickAgent>());
    }

    public void ResetStamina()
    {
        _stamina = MaxStamina;
        _animator.speed = StaminaMultiplier;
    }

    #endregion

    #region Public Properties

    public bool IsGrounded => _grounded;
    public int JumpCount => _jumps;
    public int DodgeCount => _dodges;
    public bool IsDodging => _dodging;

    public int AnimationStateIndex
    {
        get
        {
            var stateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            if (stateHash == NLightHash)
            {
                return 0;
            }

            if (stateHash == SLightHash)
            {
                return 1;
            }

            if (stateHash == DLightHash)
            {
                return 2;
            }

            if (stateHash == NAirHash)
            {
                return 3;
            }

            if (stateHash == SAirHash)
            {
                return 4;
            }

            if (stateHash == DAirHash)
            {
                return 5;
            }

            if (stateHash == DashHash)
            {
                return 6;
            }

            if (stateHash == DodgeHash)
            {
                return 7;
            }

            return -1;
        }
    }

    #endregion

    #region Collision

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckGroundCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!_grounded)
        {
            CheckGroundCollision(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            _grounded = false;
        }
    }

    private void CheckGroundCollision(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            _grounded = true;
            _canJump = true;
            _jumps = 2;
            _dodges = 3;
            _jumpInitiated = false;
        }
    }

    #endregion

    private void Awake()
    {
        if (_academy == null)
        {
            _academy = GetComponentInParent<TrainingAcademy>();
        }

        if (TrainingModeChecker.IsTrainingMode)
        {
            return;
        }

        var barObject = new GameObject("StaminaBar");
        barObject.transform.SetParent(transform);
        _staminaBar = barObject.AddComponent<StaminaBar>();
        _staminaBar.Initialize(this, _staminaBarOffset);
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _animator.updateMode = AnimatorUpdateMode.Fixed;
        _gravity = _rb.gravityScale;

        if (TrainingModeChecker.IsTrainingMode)
        {
            return;
        }

        Camera.main.GetComponent<CameraFollow>().AddPlayer(this);
    }

    public void ChangeAppearance(string name, Color color)
    {
        _playerName = name;
        _color = color;

        NameChanged(name, name);
        ColorChanged(color, color);
    }

    private void NameChanged(string oldName, string newName)
    {
        if (_nameText != null) _nameText.text = newName;
    }

    private void ColorChanged(Color oldColor, Color newColor)
    {
        if (_limbs == null) return;
        foreach (var limb in _limbs)
        {
            limb.color = newColor;
        }
    }

    private void HandleInput()
    {
        if (_device == null) return;

        if (_device.JumpDown) Jump();
        if (_device.DashDown) Dash();
        if (_device.FastFallDown) FastFall();
        if (_device.AttackDown) Attack();
        if (_device.UpDown) Up();
        if (_device.UpUp) RetractUp();
        if (_device.DownDown) Down();
        if (_device.DownUp) RetractDown();
        if (_device.LeftDown) Left();
        if (_device.LeftUp) RetractLeft();
        if (_device.RightDown) Right();
        if (_device.RightUp) RetractRight();
    }

    private void Update()
    {
        HandleInput();

        if (_launched)
        {
            transform.right = -_rb.linearVelocity;
            return;
        }
    }

    private void FixedUpdate()
    {
        HandleInput();

        if (_launched)
        {
            return;
        }

        _rb.gravityScale = _dodging ? 0 : _gravity;
        if (_dodging)
        {
            _animator.speed = 1f;
            return;
        }

        if (_rb.linearVelocity.y < -_maxFallSpeed)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_maxFallSpeed);
        }

        if (!_grounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * (1 - Time.fixedDeltaTime * _airDrag), _rb.linearVelocity.y);
        }

        if (!_controlling)
        {
            return;
        }

        var stateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        var isDashing = _dashInitiated || stateHash == DashHash;
        var isDodging = _dodging || stateHash == DodgeHash;

        if (!_attacking && !isDashing && !isDodging && _stamina < MaxStamina)
        {
            _stamina = Mathf.Min(MaxStamina, _stamina + _staminaRegenRate * Time.fixedDeltaTime);
        }

        if (isDodging)
        {
            _animator.speed = 1f;
        }
        else
        {
            _animator.speed = StaminaMultiplier;
        }

        var xDir = (_right ? 1 : 0) - (_left ? 1 : 0);
        _animator.SetBool("run", xDir != 0);

        RunJump();

        RunFastFall();

        _animator.SetBool("inAir", !_grounded);

        if (_serverAttack)
        {
            _serverAttack = false;
            if (_attacking || _dodging)
            {
                return;
            }

            _stamina = Mathf.Max(MinStamina, _stamina - StaminaStep);
            _animator.speed = StaminaMultiplier;

            InitiateAttack();

            var dir = new Vector2(
                (_right ? 1 : 0) - (_left ? 1 : 0),
                (_up ? 1 : 0) - (_down ? 1 : 0)
            );

            if (dir.x != 0)
            {
                _animator.SetTrigger("side");
            }
            else if (dir.y < 0)
            {
                _animator.SetTrigger("down");
            }
            else
            {
                _animator.SetTrigger("up");
            }

            if (xDir != 0)
            {
                transform.eulerAngles = new Vector3(0, xDir > 0 ? 0 : 180, 0);
            }
        }

        if (_dashInitiated)
        {
            return;
        }

        if (_grounded && !_jumpInitiated && !_attacking)
        {
            if (_serverDash)
            {
                var dir = new Vector2(
                    (_right ? 1 : 0) - (_left ? 1 : 0),
                    (_up ? 1 : 0) - (_down ? 1 : 0)
                );

                if (dir == Vector2.zero || dir == Vector2.up)
                {
                    var timeSinceLastDodge = Time.time - _lastDodgeTime;
                    if (stateHash != DodgeHash && timeSinceLastDodge >= DodgeCooldown)
                    {
                        _lastDodgeTime = Time.time;
                        _animator.speed = 1f;
                        _animator.SetTrigger("dodge");
                        if (dir == Vector2.up)
                        {
                            _dodges--;
                        }
                        _canRecastDodge = false;
                    }
                }
                else
                {
                    var timeSinceLastDash = Time.time - _lastDashTime;
                    if (timeSinceLastDash >= DashCooldown)
                    {
                        _lastDashTime = Time.time;
                        _dashInitiated = _serverDash;
                        _animator.SetTrigger("dash");
                    }
                }
            }

            if (!_dodging)
            {
                _rb.linearVelocity = new Vector2(xDir * _movementSpeed * StaminaMultiplier, _rb.linearVelocity.y);
            }
        }
        else if ((_canRecastDodge || !_dodging) && !_grounded)
        {
            if (!_dodging)
            {
                _rb.AddForce(new Vector2(xDir * _airAcceleration * Time.fixedDeltaTime, 0));
            }

            if (_serverDash && _dodges > 0 && !_attacking)
            {
                var timeSinceLastDodge = Time.time - _lastDodgeTime;
                if (timeSinceLastDodge >= DodgeCooldown)
                {
                    _lastDodgeTime = Time.time;
                    _dodges--;
                    _animator.speed = 1f;
                    _animator.SetTrigger("dodge");
                    _canRecastDodge = !_serverDash;
                }
            }
        }

        _serverDash = false;

        if (xDir != 0 && !_dodging && !_attacking)
        {
            transform.eulerAngles = new Vector3(0, xDir > 0 ? 0 : 180, 0);
        }
    }

    public void SetInputDevice(InputDevice device)
    {
        _device = device;
        _controlling = true;
    }

    public void SetControlling(bool controlling)
    {
        _controlling = controlling;
    }

    private void RunJump()
    {
        if (_serverJump)
        {
            _serverJump = false;
            var timeSinceLastJump = Time.time - _lastJumpTime;
            if (!((_jumps != 0 || _grounded) && _canJump && !_dodging && !_attacking && timeSinceLastJump >= JumpCooldown))
            {
                return;
            }

            _lastJumpTime = Time.time;
            _jumpInitiated = true;
            _animator.SetTrigger("jump");
            _canJump = false;
            if (!_grounded)
            {
                _jumps--;
            }
        }
    }

    private void RunFastFall()
    {
        if (_serverFastFall)
        {
            var timeSinceLastJump = Time.time - _lastJumpTime;
            _serverFastFall = false;
            if (_grounded || _dodging || _attacking || timeSinceLastJump < JumpCooldown)
            {
                return;
            }

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_maxFallSpeed);
        }
    }

    public void Reset()
    {
        ActivateInput();
        _animator.Play("idle");
    }
}
