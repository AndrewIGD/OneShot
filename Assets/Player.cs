using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] AudioSource[] sounds;

    [SerializeField] private float movementSpeed;

    [SerializeField] private float dashSpeed;

    [SerializeField] private float jumpHeight;

    [SerializeField] private float maxFallSpeed;

    [SerializeField] private float airAcceleration;

    [SerializeField] private float airDrag;

    [SerializeField] private float dodgeSpeed;

    [SerializeField] private bool controlling;

    [SerializeField] private PhysicsMaterial2D bounceMaterial;

    [SerializeField] private TextMeshPro nameText;

    [SerializeField] private SpriteRenderer[] limbs;

    #endregion

    #region Controls

    private InputDevice _device;

    private bool _up = false;

    private bool _down = false;

    private bool _left = false;

    private bool _right = false;

    private bool _serverJump = false;

    private bool _serverDash = false;

    private bool _serverAttack = false;

    private bool _serverFastFall = false;

    protected void Jump()
    {
        if (_launched)
            return;

        _serverJump = true;
    }

    protected void Dash()
    {
        if (_launched)
            return;

        _serverDash = true;
    }

    protected void FastFall()
    {
        if (_launched)
            return;

        _serverFastFall = true;
    }

    protected void Attack()
    {
        if (_launched)
            return;

        _serverAttack = true;
    }

    protected void Up()
    {
        _up = true;
    }

    protected void RetractUp()
    {
        _up = false;
    }

    protected void Down()
    {
        _down = true;
    }

    protected void RetractDown()
    {
        _down = false;
    }

    protected void Left()
    {
        _left = true;
    }

    protected void RetractLeft()
    {
        _left = false;
    }

    protected void Right()
    {
        _right = true;
    }

    protected void RetractRight()
    {
        _right = false;
    }

    #endregion

    #region Private Fields

    private string name;

    private Color color;

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

    private bool CanMove => _animator.GetCurrentAnimatorStateInfo(0).IsName("idle") || _animator.GetCurrentAnimatorStateInfo(0).IsName("run");

    #endregion

    #region Components

    private Rigidbody2D _rb;

    private Animator _animator;

    #endregion

    #region Animator Methods

    private void PlaySound(int id) => sounds[id].Play();

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

        /*_animator.SetBool("run", false);
        _animator.SetBool("dash", false);
        _animator.SetBool("jump", false);
        _animator.SetBool("inAir", false);
        _animator.SetBool("dodge", false);
        _animator.SetBool("side", false);
        _animator.SetBool("up", false);
        _animator.SetBool("down", false);*/
    }

    private void ApplyDashSpeed()
    {
        _rb.linearVelocity = new Vector2((transform.eulerAngles.y > 90 ? -1 : 1) * dashSpeed, _rb.linearVelocity.y);

        //_animator.SetBool("dash", false);
    }

    private void CancelHorizontalMomentum()
    {
        _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);

        _dashInitiated = false;
    }

    private void DodgeDir()
    {
        if (_launched)
            return;

        Vector2 dir = Vector2.zero;

        dir.x += _right ? 1 : 0;
        dir.x -= _left ? 1 : 0;
        dir.y += _up ? 1 : 0;
        dir.y -= _down ? 1 : 0;

        if (_grounded)
        {
            if (dir == Vector2.zero || dir == new Vector2(0, 1))
            {
                _rb.linearVelocity = dir * dodgeSpeed;
            }
        }
        else _rb.linearVelocity = dir * dodgeSpeed;

        _dodging = true;

        //_animator.SetBool("dodge", false);

        gameObject.layer = LayerMask.NameToLayer("PlayerNoHit");
    }

    private void RecastDodge()
    {
        if (_launched)
            return;

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
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpHeight);

        //_animator.SetBool("jump", false);

        _dashInitiated = false;

        _jumpInitiated = true;

        _canJump = true;
    }

    private void InitiateAttack()
    {
        /*_animator.SetBool("side", false);
        _animator.SetBool("up", false);
        _animator.SetBool("down", false);*/

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

        _rb.sharedMaterial = bounceMaterial;

        _animator.SetTrigger("launch");
    }

    #endregion

    #region Collision

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.tag == "Ground")
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
        if (collision.transform.tag == "Ground")
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
        this.name = name;
        this.color = color;
    }

    private void NameChanged(string oldName, string newName)
    {
        nameText.text = newName;
    }

    private void ColorChanged(Color oldColor, Color newColor)
    {
        for (int i = 0; i < limbs.Length; i++)
        {
            limbs[i].color = newColor;
        }
    }

    private void Update()
    {
        if (_device.JumpDown)
                Jump();

            if (_device.DashDown)
                Dash();

            if (_device.FastFallDown)
                FastFall();

            if (_device.AttackDown)
                Attack();

            if (_device.UpDown)
                Up();

            if (_device.UpUp)
                RetractUp();

            if (_device.DownDown)
                Down();

            if (_device.DownUp)
                RetractDown();

            if (_device.LeftDown)
                Left();

            if (_device.LeftUp)
                RetractLeft();

            if (_device.RightDown)
                Right();

            if (_device.RightUp)
                RetractRight();

        if (_launched)
        {
            transform.right = -_rb.linearVelocity;

            return;
        }

        if (controlling == false)
            return;

        int xDir = 0;

        xDir -= _left ? 1 : 0;
        xDir += _right ? 1 : 0;

        _animator.SetBool("run", xDir != 0);

        if (_serverJump)
        {
            _serverJump = false;

            if (!((_jumps != 0 || _grounded) && _canJump && _dodging == false && _attacking == false))
                return;

            _jumpInitiated = true;

            _animator.SetTrigger("jump");

            _canJump = false;

            if (!_grounded)
                _jumps--;
        }

        if (_serverFastFall)
        {
            _serverFastFall = false;

            if (!(!_grounded && _dodging == false && _attacking == false))
                return;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -maxFallSpeed);
        }

         _animator.SetBool("inAir", !_grounded);

        if (_serverAttack)
        {
            _serverAttack = false;

            if (!(_attacking == false && _dodging == false))
                return;

            Vector2 dir = Vector2.zero;

            dir.x += _right ? 1 : 0;
            dir.x -= _left ? 1 : 0;
            dir.y += _up ? 1 : 0;
            dir.y -= _down ? 1 : 0;

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

        if (_grounded && _jumpInitiated == false && _attacking == false)
        {
            if (_serverDash)
            {
                Vector2 dir = Vector2.zero;

                dir.x += _right ? 1 : 0;
                dir.x -= _left ? 1 : 0;
                dir.y += _up ? 1 : 0;
                dir.y -= _down ? 1 : 0;

                if (dir == Vector2.zero || dir == new Vector2(0, 1))
                {
                    if (_animator.GetCurrentAnimatorStateInfo(0).IsName("dodge") == false)
                    {
                        _animator.SetTrigger("dodge");

                        if(dir == new Vector2(0, 1))
                            _dodges--;

                        _canRecastDodge = false;
                    }
                }
                else
                {
                    _dashInitiated = _serverDash;

                    _animator.SetTrigger("dash");
                }
            }

            if (_dodging == false)
                _rb.linearVelocity = new Vector2(xDir * movementSpeed, _rb.linearVelocity.y);
        }
        else if ((_canRecastDodge || _dodging == false) && _grounded == false)
        {
            if (_dodging == false)
                _rb.AddForce(new Vector2(xDir * airAcceleration * Time.deltaTime, 0));

            if (_serverDash && _dodges > 0 && _attacking == false)
            {
                _dodges--;

                _animator.SetTrigger("dodge");

                _canRecastDodge = !_serverDash;
            }
        }

        _serverDash = false;

        if (xDir != 0 && _dodging == false && _attacking == false)
            transform.eulerAngles = new Vector3(0, xDir > 0 ? 0 : 180, 0);
    }

    private void FixedUpdate()
    {
        if (_launched)
            return;

        _rb.gravityScale = _dodging ? 0 : _gravity;
        if (_dodging)
            return;

        if (_rb.linearVelocity.y < -maxFallSpeed)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -maxFallSpeed);

        if (!_grounded)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * (1 - Time.fixedDeltaTime * airDrag), _rb.linearVelocity.y);
    }

    public void SetInputDevice(InputDevice device)
    {
        _device = device;
    }
}
