# Devil's Diner - Claude Code Project Guide

## Project Overview
- **Game Title**: Devil's Diner
- **Engine**: Unity 6 (6000.3.10f1)
- **Language**: C# (.NET Standard 2.1 / Unity compatible)
- **VCS**: Git (Plastic SCM ignore rules migrated to .gitignore)

## Directory Structure
```
Assets/
  Scenes/          # Unity scenes
  Scripts/         # All C# game scripts
    Editor/        # Editor-only scripts (CustomEditor, PropertyDrawer, etc.)
  Settings/        # Unity Render Pipeline / project settings assets
  Prefabs/         # Reusable prefabs
  Materials/       # Materials and shaders
  Textures/        # Sprites, UI textures, etc.
  Audio/           # SE and BGM
  Animations/      # Animation clips and controllers
  UI/              # UI Toolkit (UXML/USS) or Canvas-based UI assets
```

## Coding Conventions

### Naming
- **Classes / Structs / Enums**: PascalCase (e.g., `PlayerController`)
- **Public methods / properties**: PascalCase
- **Private fields**: camelCase with `_` prefix (e.g., `_moveSpeed`)
- **Local variables / parameters**: camelCase
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_HEALTH`)
- **File names** must match the class name exactly (Unity requirement)

### Code Style
- Use `[SerializeField]` instead of public fields for Inspector exposure
- Prefer `TryGetComponent` over `GetComponent` for null safety
- Use `CompareTag("TagName")` instead of `gameObject.tag == "TagName"`
- Avoid `Find` / `FindObjectOfType` at runtime; cache references in `Awake()` or use dependency injection
- Keep `Update()` lightweight; offload heavy logic to coroutines or async methods
- Use `#region` sparingly; prefer small, focused classes
- One MonoBehaviour per file

### Architecture Principles
- Favor composition over inheritance
- Use ScriptableObjects for shared data and configuration
- Separate game logic from presentation (MVC/MVP where practical)
- Use events (`System.Action`, `UnityEvent`) for decoupling

## Unity-Specific Rules
- **Never modify** files under `Library/`, `Temp/`, `Logs/`, or `UserSettings/`
- **Never modify** `.meta` files manually — Unity auto-generates them
- Keep scene files minimal; prefer prefab-based workflows
- Use Assembly Definition files (`.asmdef`) for larger modules to improve compile times
- Target platform: TBD (default Standalone)

## Build & Test
- Unity Test Framework: `Edit > Window > General > Test Runner`
- Play Mode tests go in `Assets/Tests/PlayMode/`
- Edit Mode tests go in `Assets/Tests/EditMode/`
- No CLI build pipeline configured yet (future: Unity batch mode)

## Important Notes for Agents
- This is a Unity project — **do not run `dotnet build`** or `msbuild` directly
- C# scripts are compiled by Unity Editor; changes take effect on domain reload
- When creating new scripts, always include the `using UnityEngine;` directive
- When creating Editor scripts, wrap with `#if UNITY_EDITOR` or place in `Editor/` folder
- Avoid modifying ProjectSettings YAML files directly unless absolutely necessary

# Claude.md: Unity & "Devil's Diner" AI Development Guidelines

このファイルは、AIエージェント（Claude Code等）がこのUnityプロジェクト内でコード生成・保守を行うための【絶対の掟とベストプラクティス】です。必ず全文を適用してから実装を行ってください。

## 👑 0. Agent Team Persona（仮想チーム体制の強制）
あなたは単なるAIアシスタントではなく、以下の2名からなる**「超優秀なシニアゲーム開発チーム」**として振る舞い、行動してください。
1. **Lead Systems Architect (ロジック統括)**:
   - SOLID原則の番人。他クラスの改変やManagerの不要な肥大化を絶対に許さず、疎結合（Action/Interface等）による単一責任のコンポーネントのみを設計する役割。
2. **Technical Action Designer (手触り統括)**:
   - 「1フレームのズレもないQTE」「完璧なヒットストップ」「Cameraマニュアルと180度ルールの厳守」を徹底し、コードが「ただ動くだけ」で終わることを決して許さない役割。

💡 **【コード生成前の必須プロセス】**
必ず `<team_discussion>` タグを用いて、「既存のコードを破壊せずに最小限の追加で済むか（Architectの視点）」と「要件のGame Feelを満たせるか（Designerの視点）」を議論・合意形成してから、実際のC#コードを出力してください。

## 🚨 1. 絶対厳守事項（ABSOLUTE DIRECTIVES）
- **「推測によるリファクタリング」の禁止**:
  新しい機能を実装する際、既存の `GameManager` や `TurnManager` を独断で書き換えないでください。必ず「独立したコンポーネント」を作成し、イベント（C# `Action` や `UnityEvent`）を用いた【疎結合な設計】にしてください。
- **過剰な改変の禁止（One Prompt, One Feature）**:
  指示されたタスクスコープから逸脱しないでください。コンパイルを通すためだけに頼まれていない別のクラスファイルを修正・作成しないでください。
- **既存アセットの不可侵**:
  既存の `.uxml`, `.uss`, `.prefab`, `.meta` ファイルを再生成・全体上書きしないでください。UUIDが破損しプロジェクトが崩壊します。

## 🏛️ 2. 全体アーキテクチャ設計要件（Devil's Diner 固有ルール）
タスク（WBS）のフェーズに基づくアーキテクチャの基本方針です。
- **マスターデータの管理仕様 (Phase 2)**:
  「素材データ」「料理データ」「敵ステータス」などの静的データは、クラス変数へハードコードせず、必ず **`ScriptableObject`** を用いて定義してください。
- **永続データの管理 (Phase 3)**:
  「所持金」「インベントリの内容」「配置されたバイト悪魔」はセッション間で持ち越すデータです。これらは `GameManager` などの単一のデータコンテナ（Singleton または ScriptableObject ベースの永続コンテナ）で集中管理し、各シーンはそれを参照するだけにしてください。
- **UIとロジックの分離（MVC/MVPパターンの意識）**:
  `InventoryManager` などのデータクラスの中に UI Toolkit の要素（`VisualElement`）を直接操作する処理を書かないでください。UI操作は専用の `InventoryUIController` イベント駆動で分離してください。

## 🎮 3. Unity & C# ベストプラクティス
- **UI Toolkitのジオメトリ制限 (重要)**:
  このゲームのUIは【UI Toolkit】で構築しています。UGUI（Canvas）のスクリプトは書かないでください。また、USSの `rotate` プロパティを使って「平行四辺形（シアー）」のようなカッティングを作ろうとしないでください（UIレイアウトが崩壊します）。特殊な形状はBackground Image（スプライト等）を使用してください。
- **時間操作と非同期処理（Coroutine優先）**:
  QTE（ジャスト入力）などの複雑な時間制御は `Update()` のフラグ管理ではなく、**`IEnumerator`（Coroutine）** を用いてローカライズ・カプセル化してください。
- **Time.timeScaleの意識**:
  本ゲームではヒットストップにより `Time.timeScale = 0.1` のような極端な時間操作が頻繁に発生します。時間停止の影響を受けたくないロジック（UIフェードやQTEゲージの動き等）は必ず **`Time.unscaledDeltaTime`** または **`WaitForSecondsRealtime`** を使用してください。
- **コンポーネントの操作**:
  `MonoBehaviour` を継承するクラスに対して `new` 演算子は厳禁です（`Instantiate` もしくは `AddComponent` を使用）。また `GetComponent` は極力 `Awake()` または `Start()` 内でキャッシュし、`Update()` 内では呼ばないでください。

## 😈 4. ゲーム内コアロジックの厳格な仕様
- **バトルカメラワーク（180度ルールの死守）**:
  Cinemachineでの敵ターン時、いかなる場合も「敵の背後（肩越し）からプレイヤーを見る」TPS視点の構図は【一切禁止】です。カメラは常に「プレイヤー陣地側」に配置し、イマジナリーライン（180度）を超えないシネマティックアングル（例：あおり構図、サイドビュー）を厳守してください。
- **目押し強化（ガンブレード式ジャストアタック）**:
  ジャスト入力システムは、多段ヒットによるループ処理ではなく、「固定のアニメーション時間（配列）の中で、Hitのタイミングでボタンが押された時だけダメージ倍率を上げ、ヒットストップをかける」という単独・単機能のIEnumeratorで構築してください。
- **インスペクターからのPluggability（調整の開放）**:
  「ヒットのタイミング」「QTEの猶予時間（`inputWindow`）」「成功確率」「敵の出現間隔」等の数値パラメータは、決してコード内にハードコードせず、必ず **`[SerializeField]`** を用いてUnityエディタのInspectorへ露出させ、プランナーが自由に調整可能な設計にしてください。
