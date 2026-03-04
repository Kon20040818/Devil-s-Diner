// ============================================================
// RevolverMenuController.cs
// ガンマンリボルバー風の回転式バトルコマンドメニュー。
// UI Toolkit (UIDocument) ベースで、シリンダーを回転させて
// コマンドを選択する。画面左下に配置。
// コマンド確定後のターゲット選択もこのクラスが担当する。
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// リボルバー（回転弾倉）風のバトルコマンド選択UI。
/// UIDocument にアタッチして使用する。
/// 左右入力でシリンダーを回転させ、決定キーでコマンド確定。
/// 確定後はターゲット選択に遷移し、最終的に BattleManager へ通知する。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class RevolverMenuController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // メニュー項目定義
    // ──────────────────────────────────────────────

    /// <summary>リボルバーメニューの1項目。</summary>
    [Serializable]
    public class MenuItem
    {
        public string Label;
        public CharacterBattleController.ActionType ActionType;
        /// <summary>使用不可時に true にする。</summary>
        [NonSerialized] public bool Disabled;
    }

    /// <summary>メニューの表示モード。</summary>
    private enum MenuMode
    {
        Command,
        TargetSelect
    }

    // ──────────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────────

    [Header("配置設定")]
    [Tooltip("弾丸を配置する半径 (px)")]
    [SerializeField] private float _radius = 90f;

    [Header("メニュー項目")]
    [SerializeField] private List<MenuItem> _menuItems = new List<MenuItem>
    {
        new MenuItem { Label = "攻撃",  ActionType = CharacterBattleController.ActionType.BasicAttack },
        new MenuItem { Label = "スキル", ActionType = CharacterBattleController.ActionType.Skill },
        new MenuItem { Label = "必殺技", ActionType = CharacterBattleController.ActionType.Ultimate },
    };

    // ──────────────────────────────────────────────
    // ランタイム
    // ──────────────────────────────────────────────

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _cylinder;
    private Label _characterNameLabel;
    private Label _costLabel;

    private readonly List<VisualElement> _bulletElements = new List<VisualElement>();

    private int _selectedIndex;
    private float _currentAngle;
    private bool _isVisible;
    private MenuMode _mode = MenuMode.Command;

    // 遅延Show用バッファ
    private bool _pendingShow;
    private bool _pendingCanUseSkill;
    private bool _pendingCanUseUltimate;

    private BattleManager _battleManager;
    private CharacterBattleController _currentCharacter;

    // ターゲット選択用
    private CharacterBattleController.ActionType _pendingAction;
    private readonly List<CharacterBattleController> _targetList = new List<CharacterBattleController>();
    private int _targetIndex;

    // ──────────────────────────────────────────────
    // イベント
    // ──────────────────────────────────────────────

    /// <summary>コマンドが確定したとき (ActionType)。</summary>
    public event Action<CharacterBattleController.ActionType> OnCommandSelected;

    /// <summary>ターゲットが確定したとき (ActionType, Target)。</summary>
    public event Action<CharacterBattleController.ActionType, CharacterBattleController> OnTargetConfirmed;

    // ──────────────────────────────────────────────
    // 公開 API
    // ──────────────────────────────────────────────

    /// <summary>BattleManager を注入して初期化する。</summary>
    public void Initialize(BattleManager battleManager)
    {
        _battleManager = battleManager;

        // ターゲット確定時に BattleManager へアクションを実行
        OnTargetConfirmed += (actionType, target) =>
        {
            _battleManager.ExecutePlayerAction(actionType, target);
        };
    }

    /// <summary>
    /// リボルバーメニューを表示する（コマンド選択モード）。
    /// コマンド選択フェーズの開始時に BattleUIManager から呼ぶ。
    /// </summary>
    public void Show(CharacterBattleController character, bool canUseSkill, bool canUseUltimate)
    {
        _currentCharacter = character;
        _mode = MenuMode.Command;

        if (!TryResolveUI())
        {
            // UI未構築の場合はリクエストをバッファして次フレームでリトライ
            Debug.Log("[RevolverMenu] UI要素が未構築です。次フレームでリトライします。");
            _pendingShow = true;
            _pendingCanUseSkill = canUseSkill;
            _pendingCanUseUltimate = canUseUltimate;
            return;
        }

        _pendingShow = false;
        _isVisible = true;

        // 使用可否を反映
        foreach (var item in _menuItems)
        {
            switch (item.ActionType)
            {
                case CharacterBattleController.ActionType.Skill:
                    item.Disabled = !canUseSkill;
                    break;
                case CharacterBattleController.ActionType.Ultimate:
                    item.Disabled = !canUseUltimate;
                    break;
                default:
                    item.Disabled = false;
                    break;
            }
        }

        // 選択を先頭にリセット
        _selectedIndex = 0;

        RebuildBullets();
        UpdateRotation(immediate: true);
        UpdateInfoPanel();

        _root.RemoveFromClassList("hidden");
    }

    /// <summary>リボルバーメニューを非表示にする。</summary>
    public void Hide()
    {
        _isVisible = false;
        _pendingShow = false;
        _mode = MenuMode.Command;
        if (_root != null) _root.AddToClassList("hidden");
    }

    /// <summary>
    /// ターゲット選択モードへ移行する。
    /// BattleUIManager の HandleCommandSelected から呼ばれる。
    /// </summary>
    public void EnterTargetSelection(
        CharacterBattleController.ActionType actionType,
        IReadOnlyList<CharacterBattleController> enemies)
    {
        _pendingAction = actionType;
        _targetList.Clear();
        _targetIndex = 0;

        foreach (var e in enemies)
        {
            if (e != null && e.IsAlive)
                _targetList.Add(e);
        }

        if (_targetList.Count == 0)
        {
            Debug.LogWarning("[RevolverMenu] 生存中のターゲットがいません。");
            return;
        }

        _mode = MenuMode.TargetSelect;

        // シリンダーをターゲットリストで再構築
        RebuildTargetBullets();
        _selectedIndex = 0;
        UpdateRotation(immediate: true);
        UpdateTargetInfoPanel();
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private bool _uiResolved;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // UIDocument の rootVisualElement は OnEnable 時点ではまだ
        // 構築されていない場合があるため、初回クエリは遅延させる。
        _uiResolved = false;
    }

    /// <summary>UIDocument の Visual Tree から要素を取得する。</summary>
    private bool TryResolveUI()
    {
        if (_uiResolved) return _root != null;
        if (_uiDocument == null) return false;

        var rootVE = _uiDocument.rootVisualElement;
        if (rootVE == null) return false;

        _root = rootVE.Q<VisualElement>("revolver-root");
        _cylinder = rootVE.Q<VisualElement>("cylinder");
        _characterNameLabel = rootVE.Q<Label>("character-name");
        _costLabel = rootVE.Q<Label>("cost-label");

        _uiResolved = _root != null && _cylinder != null;

        if (_uiResolved)
        {
            // 初期状態は非表示
            _root.AddToClassList("hidden");
            Debug.Log("[RevolverMenu] UI要素の取得に成功しました。");
        }

        return _uiResolved;
    }

    private void Update()
    {
        // 遅延Show：UI未構築だった場合に次フレームでリトライ
        if (_pendingShow)
        {
            Show(_currentCharacter, _pendingCanUseSkill, _pendingCanUseUltimate);
            return;
        }

        if (!_isVisible) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // ── 左右回転 ──
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
        {
            RotateLeft();
        }
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
        {
            RotateRight();
        }

        // ── 決定 ──
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            if (_mode == MenuMode.Command)
                ConfirmCommand();
            else
                ConfirmTarget();
        }

        // ── キャンセル（ターゲット選択→コマンド選択に戻る） ──
        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (_mode == MenuMode.TargetSelect)
            {
                ReturnToCommandMode();
            }
        }
    }

    // ──────────────────────────────────────────────
    // 回転操作
    // ──────────────────────────────────────────────

    private void RotateLeft()
    {
        int count = _mode == MenuMode.Command ? _menuItems.Count : _targetList.Count;
        if (count == 0) return;
        _selectedIndex = (_selectedIndex - 1 + count) % count;
        UpdateRotation(immediate: false);

        if (_mode == MenuMode.Command)
            UpdateInfoPanel();
        else
            UpdateTargetInfoPanel();
    }

    private void RotateRight()
    {
        int count = _mode == MenuMode.Command ? _menuItems.Count : _targetList.Count;
        if (count == 0) return;
        _selectedIndex = (_selectedIndex + 1) % count;
        UpdateRotation(immediate: false);

        if (_mode == MenuMode.Command)
            UpdateInfoPanel();
        else
            UpdateTargetInfoPanel();
    }

    /// <summary>
    /// シリンダーの回転角を更新する。
    /// immediate=true のときは transition なしで即座に設定する。
    /// </summary>
    private void UpdateRotation(bool immediate)
    {
        int count = _mode == MenuMode.Command ? _menuItems.Count : _targetList.Count;
        if (_cylinder == null || count == 0) return;

        float anglePerItem = 360f / count;
        _currentAngle = -_selectedIndex * anglePerItem;

        if (immediate)
        {
            // transition を一時的に無効化して即座に回転
            _cylinder.style.transitionDuration = new List<TimeValue> { new TimeValue(0, TimeUnit.Second) };
            _cylinder.style.rotate = new StyleRotate(new Rotate(_currentAngle));

            // 次フレームで transition を復元
            _cylinder.schedule.Execute(() =>
            {
                _cylinder.style.transitionDuration = StyleKeyword.Null;
            });
        }
        else
        {
            _cylinder.style.rotate = new StyleRotate(new Rotate(_currentAngle));
        }

        // .selected クラスの更新
        for (int i = 0; i < _bulletElements.Count; i++)
        {
            if (i == _selectedIndex)
                _bulletElements[i].AddToClassList("selected");
            else
                _bulletElements[i].RemoveFromClassList("selected");
        }
    }

    // ──────────────────────────────────────────────
    // コマンド決定
    // ──────────────────────────────────────────────

    private void ConfirmCommand()
    {
        if (_menuItems.Count == 0) return;

        var item = _menuItems[_selectedIndex];
        if (item.Disabled)
        {
            Debug.Log($"[RevolverMenu] {item.Label} は使用不可です。");
            return;
        }

        Debug.Log($"[RevolverMenu] コマンド確定: {item.Label}");
        OnCommandSelected?.Invoke(item.ActionType);
    }

    // ──────────────────────────────────────────────
    // ターゲット決定
    // ──────────────────────────────────────────────

    private void ConfirmTarget()
    {
        if (_targetList.Count == 0) return;

        var target = _targetList[_selectedIndex];
        Debug.Log($"[RevolverMenu] ターゲット確定: {target.DisplayName}");

        Hide();
        OnTargetConfirmed?.Invoke(_pendingAction, target);
    }

    /// <summary>ターゲット選択をキャンセルしてコマンド選択に戻る。</summary>
    private void ReturnToCommandMode()
    {
        _mode = MenuMode.Command;
        _selectedIndex = 0;

        RebuildBullets();
        UpdateRotation(immediate: true);
        UpdateInfoPanel();
    }

    // ──────────────────────────────────────────────
    // 弾丸の生成・配置（コマンドモード）
    // ──────────────────────────────────────────────

    /// <summary>
    /// メニュー項目に基づいて弾丸要素を再構築する。
    /// 各弾丸は cylinder の中心から半径 _radius の位置に
    /// 円周上に等間隔で配置される。
    /// </summary>
    private void RebuildBullets()
    {
        if (_cylinder == null) return;

        ClearBullets();

        int count = _menuItems.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        float cylinderCenter = 130f; // cylinder width/height の半分

        for (int i = 0; i < count; i++)
        {
            var item = _menuItems[i];

            var bullet = CreateBulletElement(item.Label, item.Disabled);

            // ── 円周上の位置を計算 ──
            PositionBullet(bullet, i, angleStep, cylinderCenter);

            _cylinder.Add(bullet);
            _bulletElements.Add(bullet);

            // クリックでも選択・確定できるようにする
            int capturedIndex = i;
            bullet.RegisterCallback<ClickEvent>(_ =>
            {
                _selectedIndex = capturedIndex;
                UpdateRotation(immediate: false);
                UpdateInfoPanel();
                ConfirmCommand();
            });
        }

        if (_bulletElements.Count > 0)
        {
            _bulletElements[_selectedIndex].AddToClassList("selected");
        }
    }

    // ──────────────────────────────────────────────
    // 弾丸の生成・配置（ターゲット選択モード）
    // ──────────────────────────────────────────────

    private void RebuildTargetBullets()
    {
        if (_cylinder == null) return;

        ClearBullets();

        int count = _targetList.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        float cylinderCenter = 130f;

        for (int i = 0; i < count; i++)
        {
            var target = _targetList[i];
            string label = $"{target.DisplayName}\nHP:{target.CurrentHP}/{target.MaxHP}";

            var bullet = CreateBulletElement(label, false);
            // ターゲット弾丸は赤系の見た目
            bullet.AddToClassList("target-bullet");

            PositionBullet(bullet, i, angleStep, cylinderCenter);

            _cylinder.Add(bullet);
            _bulletElements.Add(bullet);

            int capturedIndex = i;
            bullet.RegisterCallback<ClickEvent>(_ =>
            {
                _selectedIndex = capturedIndex;
                UpdateRotation(immediate: false);
                UpdateTargetInfoPanel();
                ConfirmTarget();
            });
        }

        if (_bulletElements.Count > 0)
        {
            _bulletElements[0].AddToClassList("selected");
        }
    }

    // ──────────────────────────────────────────────
    // 弾丸ヘルパー
    // ──────────────────────────────────────────────

    private VisualElement CreateBulletElement(string labelText, bool disabled)
    {
        var bullet = new VisualElement();
        bullet.AddToClassList("bullet");

        if (disabled) bullet.AddToClassList("disabled");

        var icon = new VisualElement();
        icon.AddToClassList("bullet-icon");
        bullet.Add(icon);

        var label = new Label(labelText);
        label.AddToClassList("bullet-label");
        bullet.Add(label);

        return bullet;
    }

    private void PositionBullet(VisualElement bullet, int index, float angleStep, float center)
    {
        // 0番目を上（12時方向）に配置、時計回り
        float angleDeg = index * angleStep - 90f;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float x = center + _radius * Mathf.Cos(angleRad) - 35f; // 35 = bullet幅の半分
        float y = center + _radius * Mathf.Sin(angleRad) - 35f;

        bullet.style.left = x;
        bullet.style.top = y;
    }

    private void ClearBullets()
    {
        foreach (var bullet in _bulletElements)
        {
            _cylinder.Remove(bullet);
        }
        _bulletElements.Clear();
    }

    // ──────────────────────────────────────────────
    // 情報パネル更新
    // ──────────────────────────────────────────────

    private void UpdateInfoPanel()
    {
        if (_characterNameLabel != null && _currentCharacter != null)
        {
            _characterNameLabel.text = _currentCharacter.DisplayName;
        }

        if (_costLabel != null && _menuItems.Count > 0)
        {
            var item = _menuItems[_selectedIndex];
            _costLabel.text = GetCostText(item);
        }
    }

    private void UpdateTargetInfoPanel()
    {
        if (_targetList.Count == 0) return;

        var target = _targetList[_selectedIndex];

        if (_characterNameLabel != null)
        {
            string actionName = _pendingAction switch
            {
                CharacterBattleController.ActionType.Skill => "スキル",
                CharacterBattleController.ActionType.Ultimate => "必殺技",
                _ => "攻撃"
            };
            _characterNameLabel.text = $"{actionName} → {target.DisplayName}";
        }

        if (_costLabel != null)
        {
            _costLabel.text = $"HP: {target.CurrentHP}/{target.MaxHP}  [ESC] 戻る";
        }
    }

    private string GetCostText(MenuItem item)
    {
        switch (item.ActionType)
        {
            case CharacterBattleController.ActionType.BasicAttack:
                return "SP +1";
            case CharacterBattleController.ActionType.Skill:
                if (_battleManager != null)
                    return item.Disabled
                        ? $"SP -1 (不足: {_battleManager.CurrentSP}/{_battleManager.MaxSP})"
                        : $"SP -1 (残: {_battleManager.CurrentSP}/{_battleManager.MaxSP})";
                return "SP -1";
            case CharacterBattleController.ActionType.Ultimate:
                if (_currentCharacter != null)
                    return item.Disabled
                        ? $"EP不足 ({_currentCharacter.CurrentEP}/{_currentCharacter.MaxEP})"
                        : $"EP全消費 ({_currentCharacter.CurrentEP}/{_currentCharacter.MaxEP})";
                return "EP全消費";
            default:
                return "";
        }
    }

    // ──────────────────────────────────────────────
    // LateUpdate: 弾丸の逆回転（文字が読めるように）
    // ──────────────────────────────────────────────

    private void LateUpdate()
    {
        if (!_isVisible || _bulletElements.Count == 0) return;

        // cylinder の回転分だけ各弾丸を逆回転させて
        // ラベルが常に正立するようにする
        float counterAngle = -_currentAngle;
        foreach (var bullet in _bulletElements)
        {
            bullet.style.rotate = new StyleRotate(new Rotate(counterAngle));
        }
    }
}
