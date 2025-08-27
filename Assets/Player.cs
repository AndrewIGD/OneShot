using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
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

    private bool _up = false;

    private bool _down = false;

    private bool _left = false;

    private bool _right= false;

    private bool _serverJump = false;

    private bool _serverDash = false;

    private bool _serverAttack = false;

    private bool _serverFastFall = false;

    private KeyCode Right = KeyCode.D;

    private KeyCode Left = KeyCode.A;

    private KeyCode Up = KeyCode.W;

    private KeyCode Down = KeyCode.S;

    private bool Jump => Input.GetKeyDown(KeyCode.Space);

    private bool Dash => Input.GetKeyDown(KeyCode.RightShift);

    private bool FastFall => Input.GetKeyDown(KeyCode.S);

    private bool Attack => Input.GetKeyDown(KeyCode.Slash);

    [Command]
    private void SendJump()
    {
        if (_launched)
            return;

        _serverJump = true;
    }

    [Command]
    private void SendDash()
    {
        if (_launched)
            return;

        _serverDash = true;
    }

    [Command]
    private void SendFastFall()
    {
        if (_launched)
            return;

        _serverFastFall = true;
    }

    [Command]
    private void SendAttack()
    {
        if (_launched)
            return;

        _serverAttack = true;
    }

    [Command]
    private void SendUp()
    {
        _up = true;
    }

    [Command]
    private void RetractUp()
    {
        _up = false;
    }

    [Command]
    private void SendDown()
    {
        _down = true;
    }

    [Command]
    private void RetractDown()
    {
        _down = false;
    }

    [Command]
    private void SendLeft()
    {
        _left = true;
    }

    [Command]
    private void RetractLeft()
    {
        _left = false;
    }

    [Command]
    private void SendRight()
    {
        _right = true;
    }

    [Command]
    private void RetractRight()
    {
        _right = false;
    }

    #endregion

    #region Private Fields

    [SyncVar(hook = "NameChanged")]
    private string name;

    [SyncVar(hook = "ColorChanged")]
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
    private NetworkAnimator _networkAnimator;

    #endregion

    #region Animator Methods

    private void PlaySound(int id) => sounds[id].Play();

    [ServerCallback]
    private void EnableJump() => _canJump = true;

    [ServerCallback]
    private void Respawn()
    {
        transform.position = GameManager.Instance.GetRandomSpawnPosition();

        _rb.velocity = Vector2.zero;
    }

    [ServerCallback]
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
        _rb.velocity = Vector2.zero;
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

    [ServerCallback]
    private void ApplyDashSpeed()
    {
        _rb.velocity = new Vector2((transform.eulerAngles.y > 90 ? -1 : 1) * dashSpeed, _rb.velocity.y);

        //_animator.SetBool("dash", false);
    }

    [ServerCallback]
    private void CancelHorizontalMomentum()
    {
        _rb.velocity = new Vector2(0, _rb.velocity.y);

        _dashInitiated = false;
    }

    [ServerCallback]
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
                _rb.velocity = dir * dodgeSpeed;
            }
        }
        else _rb.velocity = dir * dodgeSpeed;

        _dodging = true;

        //_animator.SetBool("dodge", false);

        gameObject.layer = LayerMask.NameToLayer("PlayerNoHit");
    }

    [ServerCallback]
    private void RecastDodge()
    {
        if (_launched)
            return;

        _canRecastDodge = true;

        _rb.velocity = Vector2.zero;
    }

    [ServerCallback]
    private void StopDodge()
    {
        _dodging = false;

        gameObject.layer = LayerMask.NameToLayer("Player");
    }

    [ServerCallback]
    private void ApplyJumpHeight()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, jumpHeight);

        //_animator.SetBool("jump", false);

        _dashInitiated = false;

        _jumpInitiated = true;

        _canJump = true;
    }

    [ServerCallback]
    private void InitiateAttack()
    {
        /*_animator.SetBool("side", false);
        _animator.SetBool("up", false);
        _animator.SetBool("down", false);*/

        _attacking = true;

        _dashInitiated = false;

        _jumpInitiated = false;
    }

    [ServerCallback]
    private void ActivateDrag()
    {
        _rb.drag = 4;
    }

    [ServerCallback]
    private void StopAttack()
    {
        _attacking = false;

        _rb.drag = 0;
    }

    [ServerCallback]
    private void LaunchHorizontal(float speed)
    {
        _rb.velocity = new Vector2((transform.eulerAngles.y > 90 ? -1 : 1) * speed, 0);
    }


    #endregion

    #region Public Methods

    [Server]
    public void Blast()
    {
        _networkAnimator.SetTrigger("respawn");

        _rb.velocity = Vector2.zero;

        _rb.gravityScale = 0;

        _launched = true;
    }

    [Server]
    public void Launch(Vector2 dir)
    {
        _launched = true;

        _rb.gravityScale = 1f;

        _rb.velocity = dir;

        _rb.sharedMaterial = bounceMaterial;

        _networkAnimator.SetTrigger("launch");
    }

    #endregion

    #region Collision

    [ServerCallback]
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

    [ServerCallback]
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
        _networkAnimator = GetComponent<NetworkAnimator>();

        _gravity = _rb.gravityScale;

        Camera.main.GetComponent<CameraFollow>().AddPlayer(this);
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        ChangeAppearance(UserPrefs.userName, UserPrefs.color);
    }

    [Command]
    private void ChangeAppearance(string name, Color color)
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
        if (hasAuthority)
        {
            if (Jump)
                SendJump();

            if (Dash)
                SendDash();

            if (FastFall)
                SendFastFall();

            if (Attack)
                SendAttack();

            if (Input.GetKeyDown(Up))
                SendUp();

            if (Input.GetKeyUp(Up))
                RetractUp();

            if (Input.GetKeyDown(Down))
                SendDown();

            if (Input.GetKeyUp(Down))
                RetractDown();

            if (Input.GetKeyDown(Left))
                SendLeft();

            if (Input.GetKeyUp(Left))
                RetractLeft();

            if (Input.GetKeyDown(Right))
                SendRight();

            if (Input.GetKeyUp(Right))
                RetractRight();
        }

        if (!isServer)
            return;

        if(Input.GetKeyDown(KeyCode.F))
        {
            _serverJump = true;
            _serverDash = true;
        }

        if (_launched)
        {
            transform.right = -_rb.velocity;

            return;
        }

        if (controlling == false)
            return;

        int xDir = 0;

        xDir -= _left ? 1 : 0;
        xDir += _right ? 1 : 0;

        _animator.SetBool("run", xDir != 0);

        Debug.Log(_jumps + " " + _grounded + " " + _canJump + " " + _dodging + " " + _attacking + " " + _dashInitiated);

        if (_serverJump)
        {
            _serverJump = false;

            if (!((_jumps != 0 || _grounded) && _canJump && _dodging == false && _attacking == false))
                return;

            _jumpInitiated = true;

            _networkAnimator.SetTrigger("jump");

            _canJump = false;

            if (!_grounded)
                _jumps--;
        }

        if (_serverFastFall)
        {
            _serverFastFall = false;

            if (!(!_grounded && _dodging == false && _attacking == false))
                return;

            _rb.velocity = new Vector2(_rb.velocity.x, -maxFallSpeed);
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
                _networkAnimator.SetTrigger("side");
            else if (dir.y < 0)
                _networkAnimator.SetTrigger("down");
            else
                _networkAnimator.SetTrigger("up");

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
                        _networkAnimator.SetTrigger("dodge");

                        if(dir == new Vector2(0, 1))
                            _dodges--;

                        _canRecastDodge = false;
                    }
                }
                else
                {
                    _dashInitiated = _serverDash;

                    _networkAnimator.SetTrigger("dash");
                }
            }

            if (_dodging == false)
                _rb.velocity = new Vector2(xDir * movementSpeed, _rb.velocity.y);
        }
        else if ((_canRecastDodge || _dodging == false) && _grounded == false)
        {
            if (_dodging == false)
                _rb.AddForce(new Vector2(xDir * airAcceleration * Time.deltaTime, 0));

            if (_serverDash && _dodges > 0 && _attacking == false)
            {
                _dodges--;

                _networkAnimator.SetTrigger("dodge");

                _canRecastDodge = !_serverDash;
            }
        }

        _serverDash = false;

        if (xDir != 0 && _dodging == false && _attacking == false)
            transform.eulerAngles = new Vector3(0, xDir > 0 ? 0 : 180, 0);
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (_launched)
            return;

        _rb.gravityScale = _dodging ? 0 : _gravity;
        if (_dodging)
            return;

        if (_rb.velocity.y < -maxFallSpeed)
            _rb.velocity = new Vector2(_rb.velocity.x, -maxFallSpeed);

        if (!_grounded)
            _rb.velocity = new Vector2(_rb.velocity.x * (1 - Time.fixedDeltaTime * airDrag), _rb.velocity.y);
    }
}
