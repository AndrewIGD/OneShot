using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorParamSync : NetworkBehaviour
{
    /*[SyncVar(hook = "RunUpdate")]
    private bool _run = false;
    [SyncVar(hook = "JumpUpdate")]
    private bool _jump = false;
    [SyncVar(hook = "SideUpdate")]
    private bool _side = false;
    [SyncVar(hook = "DownUpdate")]
    private bool _down = false;
    [SyncVar(hook = "UpUpdate")]
    private bool _up = false;
    [SyncVar(hook = "InAirUpdate")]
    private bool _inAir = false;
    [SyncVar(hook = "DashUpdate")]
    private bool _dash = false;
    [SyncVar(hook = "DodgeUpdate")]
    private bool _dodge = false;*/

    private Animator _animator;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public bool IsStateName(string name) => _animator.GetCurrentAnimatorStateInfo(0).IsName(name);

    [Server]
    public void SetBool(string name, bool value)
    {
        if (_animator == null)
            return;

        /*switch (name)
        {
            case "run":
                _run = value;
                break;
            case "jump":
                _jump = value;
                break;
            case "inAir":
                _inAir = value;
                break;
            case "side":
                _side = value;
                break;
            case "down":
                _down = value;
                break;
            case "up":
                _up = value;
                break;
            case "dodge":
                _dodge = value;
                break;
            case "dash":
                _dash = value;
                break;
        }*/

        _animator.SetBool(name, value);

        SetBoolRpc(name, value);
    }

    [Server]
    public void Play(string name)
    {
        if (_animator == null)
            return;

        _animator.Play(name);

        PlayRpc(name);
    }

    [ClientRpc]
    private void PlayRpc(string name)
    {
        if (_animator == null)
            return;

        _animator.Play(name);
    }

    [ClientRpc]
    private void SetBoolRpc(string name, bool value)
    {
        if (_animator == null)
            return;

        _animator.SetBool(name, value);
    }

    /*private void RunUpdate(bool oldValue, bool newValue) => _animator.SetBool("run", newValue);
    private void JumpUpdate(bool oldValue, bool newValue) => _animator.SetBool("jump", newValue);
    private void InAirUpdate(bool oldValue, bool newValue) => _animator.SetBool("inAir", newValue);
    private void DashUpdate(bool oldValue, bool newValue) => _animator.SetBool("dash", newValue);
    private void DodgeUpdate(bool oldValue, bool newValue) => _animator.SetBool("dodge", newValue);
    private void SideUpdate(bool oldValue, bool newValue) => _animator.SetBool("side", newValue);
    private void UpUpdate(bool oldValue, bool newValue) => _animator.SetBool("up", newValue);
    private void DownUpdate(bool oldValue, bool newValue) => _animator.SetBool("down", newValue);*/
}
