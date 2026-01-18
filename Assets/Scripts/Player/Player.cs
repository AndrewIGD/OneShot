using TMPro;
using UnityEngine;

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

    private bool CanMove =>
        _animator.GetCurrentAnimatorStateInfo(0).IsName("idle") ||
        _animator.GetCurrentAnimatorStateInfo(0).IsName("run");

    #endregion

    #region Components

    private Rigidbody2D _rb;
    private Animator _animator;

    #endregion

    #region Animator Methods

    private void PlaySound(int id) => _sounds[id].Play();

    private void EnableJump() => _canJump = true;

    private void Respawn()
    {
        transform.position = GameManager.Instance.GetRandomSpawnPosition();
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

        // Animator reset (if needed, uncomment)
        /*
        _animator.SetBool("run", false);
        _animator.SetBool("dash", false);
        _animator.SetBool("jump", false);
        _animator.SetBool("inAir", false);
        _animator.SetBool("dodge", false);
        _animator.SetBool("side", false);
        _animator.SetBool("up", false);
        _animator.SetBool("down", false);
        */
    }

    private void ApplyDashSpeed()
    {
        _rb.linearVelocity = new Vector2((transform.eulerAngles.y > 90 ? -1 : 1) * _dashSpeed, _rb.linearVelocity.y);
        // _animator.SetBool("dash", false);
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
        // _animator.SetBool("dodge", false);
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
        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    private void ApplyJumpHeight()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpHeight);
        // _animator.SetBool("jump", false);
        _dashInitiated = false;
        _jumpInitiated = true;
        _canJump = true;
    }

    private void InitiateAttack()
    {
        // _animator.SetBool("side", false);
        // _animator.SetBool("up", false);
        // _animator.SetBool("down", false);
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
        _launched = true;
        _rb.gravityScale = 1f;
        _rb.linearVelocity = dir;
        _rb.sharedMaterial = _bounceMaterial;
        _animator.SetTrigger("launch");
    }

    #endregion

    #region Collision

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Ground"))
        {
            _grounded = true;
            _canJump = true;
            _jumps = 2;
            _dodges = 3;
            _jumpInitiated = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Ground"))
            _grounded = false;
    }

    #endregion

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _gravity = _rb.gravityScale;

        Camera.main.GetComponent<CameraFollow>().AddPlayer(this);
    }

    public void ChangeAppearance(string name, Color color)
    {
        _playerName = name;
        _color = color;
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

        if (!_controlling)
            return;

        int xDir = (_right ? 1 : 0) - (_left ? 1 : 0);
        _animator.SetBool("run", xDir != 0);

        if (_serverJump)
        {
            _serverJump = false;
            if (!((_jumps != 0 || _grounded) && _canJump && !_dodging && !_attacking))
                return;

            _jumpInitiated = true;
            _animator.SetTrigger("jump");
            _canJump = false;
            if (!_grounded) _jumps--;
        }

        if (_serverFastFall)
        {
            _serverFastFall = false;
            if (_grounded || _dodging || _attacking)
                return;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_maxFallSpeed);
        }

        _animator.SetBool("inAir", !_grounded);

        if (_serverAttack)
        {
            _serverAttack = false;
            if (_attacking || _dodging)
                return;

            Vector2 dir = new Vector2(
                (_right ? 1 : 0) - (_left ? 1 : 0),
                (_up ? 1 : 0) - (_down ? 1 : 0)
            );

            if (dir.x != 0)
                _animator.SetTrigger("side");
            else if (dir.y < 0)
                _animator.SetTrigger("down");
            else
                _animator.SetTrigger("up");

            if (xDir != 0)
                transform.eulerAngles = new Vector3(0, xDir > 0 ? 0 : 180, 0);
        }

        if (_dashInitiated)
            return;

        if (_grounded && !_jumpInitiated && !_attacking)
        {
            if (_serverDash)
            {
                Vector2 dir = new Vector2(
                    (_right ? 1 : 0) - (_left ? 1 : 0),
                    (_up ? 1 : 0) - (_down ? 1 : 0)
                );

                if (dir == Vector2.zero || dir == Vector2.up)
                {
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("dodge"))
                    {
                        _animator.SetTrigger("dodge");
                        if (dir == Vector2.up) _dodges--;
                        _canRecastDodge = false;
                    }
                }
                else
                {
                    _dashInitiated = _serverDash;
                    _animator.SetTrigger("dash");
                }
            }

            if (!_dodging)
                _rb.linearVelocity = new Vector2(xDir * _movementSpeed, _rb.linearVelocity.y);
        }
        else if ((_canRecastDodge || !_dodging) && !_grounded)
        {
            if (!_dodging)
                _rb.AddForce(new Vector2(xDir * _airAcceleration * Time.deltaTime, 0));

            if (_serverDash && _dodges > 0 && !_attacking)
            {
                _dodges--;
                _animator.SetTrigger("dodge");
                _canRecastDodge = !_serverDash;
            }
        }

        _serverDash = false;

        if (xDir != 0 && !_dodging && !_attacking)
            transform.eulerAngles = new Vector3(0, xDir > 0 ? 0 : 180, 0);
    }

    private void FixedUpdate()
    {
        if (_launched)
            return;

        _rb.gravityScale = _dodging ? 0 : _gravity;
        if (_dodging)
            return;

        if (_rb.linearVelocity.y < -_maxFallSpeed)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_maxFallSpeed);

        if (!_grounded)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * (1 - Time.fixedDeltaTime * _airDrag), _rb.linearVelocity.y);
    }

    public void SetInputDevice(InputDevice device)
    {
        _device = device;
    }
}
