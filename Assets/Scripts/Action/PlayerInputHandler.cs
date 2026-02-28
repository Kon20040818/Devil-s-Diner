// ============================================================
// PlayerInputHandler.cs
// 入力ラッパー。InputSystem_Actions を隠蔽し、PlayerController に
// クリーンなインターフェースを提供する。
// ============================================================
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// InputSystem_Actions のライフサイクルを管理し、
/// 移動・スプリント・ジャンプ・攻撃の入力状態をプロパティ/イベントで公開する。
/// PlayerController と同じ GameObject にアタッチして使用する。
/// </summary>
public sealed class PlayerInputHandler : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>ジャンプ入力が発生したとき（1回のみ発火）。</summary>
    public event Action OnJumpTriggered;

    /// <summary>攻撃入力が発生したとき（1回のみ発火）。</summary>
    public event Action OnAttackTriggered;

    /// <summary>回避入力が発生したとき（1回のみ発火）。</summary>
    public event Action OnDodgeTriggered;

    // ──────────────────────────────────────────────
    // プロパティ
    // ──────────────────────────────────────────────

    /// <summary>現在の移動入力ベクトル。</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>スプリントが押されているか。</summary>
    public bool IsSprinting { get; private set; }

    // ──────────────────────────────────────────────
    // キャッシュ
    // ──────────────────────────────────────────────
    private InputSystem_Actions _inputActions;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();

        // 入力バインド
        _inputActions.Player.Move.performed += OnMovePerformed;
        _inputActions.Player.Move.canceled  += OnMoveCanceled;
        _inputActions.Player.Sprint.performed += OnSprintPerformed;
        _inputActions.Player.Sprint.canceled  += OnSprintCanceled;
        _inputActions.Player.Jump.performed   += OnJumpPerformed;
        _inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= OnMovePerformed;
        _inputActions.Player.Move.canceled  -= OnMoveCanceled;
        _inputActions.Player.Sprint.performed -= OnSprintPerformed;
        _inputActions.Player.Sprint.canceled  -= OnSprintCanceled;
        _inputActions.Player.Jump.performed   -= OnJumpPerformed;
        _inputActions.Player.Attack.performed -= OnAttackPerformed;

        _inputActions.Player.Disable();
    }

    private void Update()
    {
        // 回避入力のポーリング（Left Alt キー or Gamepad B ボタン）
        bool dodgePressed = false;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.leftAltKey.wasPressedThisFrame)
        {
            dodgePressed = true;
        }

        var gamepad = Gamepad.current;
        if (!dodgePressed && gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
        {
            dodgePressed = true;
        }

        if (dodgePressed)
        {
            OnDodgeTriggered?.Invoke();
        }
    }

    private void OnDestroy()
    {
        _inputActions?.Dispose();
    }

    // ──────────────────────────────────────────────
    // 入力コールバック
    // ──────────────────────────────────────────────

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        IsSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        IsSprinting = false;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        OnJumpTriggered?.Invoke();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnAttackTriggered?.Invoke();
    }
}
