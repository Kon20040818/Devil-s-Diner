# Devil's Diner — 全体サマリー & 今後用意すべきデータ一覧

## 1. プロジェクト全体像

| 項目 | 値 |
|------|-----|
| ゲームタイトル | Devil's Diner（悪魔食堂） |
| エンジン | Unity 6 (6000.3.10f1) |
| ジャンル | ターン制RPG × 経営シミュレーション |
| C# ソースファイル | 81 ファイル |
| ScriptableObject 型 | 13 種類 |
| マスターデータ | CSV 3本 + JSON 1本 |
| エディタツール | 10 メニュー項目 |
| ドキュメント | 6 ファイル（設計書・技術資料・API リファレンス等） |

---

## 2. ゲームサイクル

```
┌─────────────────────────────────────────────────────────────┐
│  BootScene（タイトル画面）                                    │
│  └→ NEW GAME / CONTINUE                                     │
└────────────────────────┬────────────────────────────────────┘
                         ↓
┌─ Day N ────────────────────────────────────────────────────┐
│                                                             │
│  ① 朝フェーズ（BaseScene）                                  │
│     料理制作 / メニュー確認 / マップ選択 / 出発              │
│                         ↓                                   │
│  ② 昼フェーズ（FieldScene → BattleScene）                   │
│     フィールド探索 → 敵シンボル接触 → ターン制バトル          │
│     ジャストアタック / ジャストガード / 食事 / スカウト        │
│     素材ドロップ / 悪魔スカウト                               │
│                         ↓                                   │
│  ③ 夜フェーズ（ManagementScene）                             │
│     食堂営業シミュレーション / スタッフ管理 / 家具購入        │
│     売上精算 / 経験値付与                                     │
│                         ↓                                   │
│              Day N+1 へループ                                │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. システム別 実装状況

### 3-A. バトルシステム（13ファイル）

| コンポーネント | 状態 | 概要 |
|---------------|------|------|
| BattleManager | 完成 | ステートマシン（Idle→PlayerTurn→EnemyTurn→Result） |
| ActionQueueSystem | 完成 | スターレイル式 AV タイムライン |
| AttackAction | 完成 | ジャストアタック（ガンブレード式タイミング） |
| EnemyAttackAction | 完成 | ジャストガード（タイミング防御） |
| MealAction + MealBuffApplier | 完成 | バトル中食事（DishInstance 消費→バフ） |
| ScoutAction | 完成 | 確率スカウト（シグモイドカーブ） |
| EnemyAIController | 完成 | 重み付きランダム行動選択 |
| BattleCameraManager | 完成 | Cinemachine 3.x / 180度ルール厳守 |
| CharacterBattleController | 完成 | Toughness/Break + 弱点属性 |
| BuffDurationTracker | 完成 | ターン経過によるバフ自動消滅 |
| SkillEffectApplier | 完成 | スキル効果の適用ロジック |
| BattleResultController | 完成 | 勝敗判定→リザルト表示→シーン遷移 |

### 3-B. 経営システム（5ファイル）

| コンポーネント | 状態 | 概要 |
|---------------|------|------|
| CookingManager | 完成 | レシピ→素材消費→品質決定→DishInstance 生成 |
| DinerService | 完成 | 営業シミュレーション（客数・満足度・売上計算） |
| StaffManager | 完成 | スタッフ雇用/解雇・給与計算・バフ集約 |
| StaffBuffRoller | 完成 | レアリティ重み付きランダムバフ抽選 |
| HousingManager | 完成 | 家具購入・設置・ボーナス計算 |

### 3-C. コアシステム（9ファイル）

| コンポーネント | 状態 | 概要 |
|---------------|------|------|
| GameManager | 完成 | シングルトン（Gold/Inventory/Staff/Phase/ChefLevel） |
| InventoryManager | 完成 | アイテム管理（素材・料理・武器） |
| SaveDataManager | 完成 | JSON シリアライズ永続化 |
| AudioManager | 完成 | BGM/SE 再生（フェードイン/アウト） |
| AudioEventConnector | 完成 | イベント駆動のオーディオ結線 |
| BootLoader | 完成 | タイトル画面→BaseScene 遷移 |
| DropResolver | 完成 | バトル報酬（通常/ジャスト素材ドロップ） |
| DebugController | 完成 | ランタイムデバッグ（所持金/素材/レベル操作） |

### 3-D. フィールドシステム（7ファイル）

| コンポーネント | 状態 | 概要 |
|---------------|------|------|
| FieldPlayerController | 完成 | WASD + マウス操作 |
| FieldCameraController | 完成 | 三人称追従カメラ |
| EnemySymbol | 完成 | フィールド敵シンボル（索敵・追跡） |
| FieldEncounterHandler | 完成 | 接触→バトル遷移 |
| 各 SceneBootstrap ×4 | 完成 | 各シーンの自動結線 + フォールバック |

### 3-E. UI システム（14ファイル）

| コンポーネント | 状態 | 概要 |
|---------------|------|------|
| DynamicBattleUIController | 完成 | Metaphor 風 UI Toolkit バトルUI |
| ActionTimelineUI | 完成 | スターレイル式行動順タイムライン |
| SkillCommandUI | 完成 | コマンドメニュー |
| CharacterStatusUI | 完成 | HP/EP ステータスカード |
| BattleResultUI | 完成 | 勝利/敗北リザルト画面 |
| TitleScreenUI | 完成 | タイトル画面（NEW/CONTINUE） |
| BaseSceneUI | 完成 | 拠点UI（料理・マップ選択・出発） |
| ManagementSceneUI | 完成 | 経営UI（営業・スタッフ・家具・料理） |
| その他 6種 | 完成 | ダメージ数字/エフェクト/必殺技ポートレート等 |

### 3-F. エディタツール（10ファイル）

| ツール | メニュー | 概要 |
|--------|----------|------|
| MasterDataImporter | Import Master Data (CSV and JSON) | CSV/JSON → ScriptableObject 変換 |
| SampleDataGenerator | Generate Sample Staff & Calendar Data | スタッフ/カレンダー/エネミー結線 |
| BootSceneAutoBuilder | Auto Setup Boot Scene | 起動シーン自動生成 |
| DevilsDinerSetupTool | Auto Setup Battle Scene | バトルシーン自動生成 |
| BaseSceneAutoBuilder | Auto Setup Base Scene | 拠点シーン自動生成 |
| FieldSceneAutoBuilder | Auto Setup Field Scene | フィールドシーン自動生成 |
| ManagementSceneAutoBuilder | Auto Setup Management Scene | 経営シーン自動生成 |
| BattleCameraSetupWindow | Setup Battle Camera | バトルカメラ設定 |
| InventoryTestTool | Inventory Test Tool | デバッグ用インベントリ操作 |
| MetaphorUISetup | Metaphor UI Setup | UI Toolkit 設定 |

---

## 4. 既存マスターデータ（完成済み）

### 素材（12種） — `ingredients.csv`

| ID | 名前 | レアリティ | ドロップ率 |
|----|------|-----------|-----------|
| ING_Meat | 生肉 | ★1 | 80% |
| ING_Vegetable | 野菜 | ★1 | 90% |
| ING_Bone | 魔骨 | ★2 | 60% |
| ING_Herb | 薬草 | ★2 | 70% |
| ING_Fish | 深淵魚 | ★2 | 65% |
| ING_Egg | 魔卵 | ★2 | 55% |
| ING_Mushroom | 冥茸 | ★2 | 60% |
| ING_Fruit | 禁断果実 | ★2 | 50% |
| ING_Scale | 竜鱗 | ★3 | 40% |
| ING_Poison | 毒腺 | ★3 | 50% |
| ING_Spice | 獄炎香辛料 | ★3 | 40% |
| ING_Crystal | 魔晶石 | ★4 | 25% |

### 料理（30品） — `dishes.csv`
肉料理8品 / 魚料理7品 / サラダ7品 / デザート8品

### レシピ（30種） — `recipes.json`
シェフLv1〜5 でアンロック

### 家具（3種） — `furniture.csv`
テーブルセット / 装飾品 / 厨房改修

### スタッフバフ（8種） — SampleDataGenerator ハードコード
品質向上 / 接客上手 / 鮮度管理 / 手際良い / 質素 / 肉料理の達人 / サラダ職人 / 超絶品質

### スタッフ種族（5種） — SampleDataGenerator ハードコード
サボテン族 / ボス族 / 竜族 / ヒドラ族 / ダミー族

### カレンダーイベント（3種） — SampleDataGenerator ハードコード
肉の日 / 収穫祭 / グルメウィーク

### 敵データ（5種） — DevilsDinerSetupTool 生成
ENM_Cactus / ENM_Boss / ENM_DragonLord / ENM_PoisonHydra / ENM_Dummy

### マップ（5種） — FieldSceneAutoBuilder 生成
砂漠 / 森 / 沼地 / 火山 / 城

---

## 5. 今後用意すべきデータ一覧

### 優先度 A — ゲームとして成立するために必須

| # | データ種別 | 現状 | 目標 | 追加数 | 作業内容 |
|---|-----------|------|------|--------|---------|
| A1 | **敵データ (EnemyData)** | 5体 | 20〜30体 | +15〜25 | マップごとに4〜6体。ステータス・弱点属性・ドロップアイテム・スカウト種族を定義。CSV化推奨 |
| A2 | **キャラクターステータス (CharacterStats)** | 2体（Hero, Slime） | 20〜30体 | +18〜28 | 敵1体につき1つ必要。HP/ATK/DEF/SPD/属性/弱点/タフネスを定義。CSV化推奨 |
| A3 | **マップデータ (MapData)** | 5マップ | 5〜8マップ | +0〜3 | 各マップに出現敵リスト・推奨レベル・環境タイプを設定。現状5で最低限は足りている |
| A4 | **プレイヤーキャラクター (CharacterStats)** | 1体（Hero） | 3〜4体 | +2〜3 | パーティメンバー追加。差別化（速度型・防御型・魔法型など） |
| A5 | **武器データ (WeaponData)** | 6種 | 10〜15種 | +4〜9 | レアリティ帯ごとに2〜3種。ジャスト入力倍率の差別化 |

### 優先度 B — ゲーム体験を豊かにするために重要

| # | データ種別 | 現状 | 目標 | 追加数 | 作業内容 |
|---|-----------|------|------|--------|---------|
| B1 | **家具データ (FurnitureData)** | 3種 | 15〜20種 | +12〜17 | カテゴリ5種（Table/Chair/Decoration/Lighting/Kitchen）×3〜4段階。価格・効果の段階的上昇。CSV追記 |
| B2 | **スタッフ種族 (StaffRaceData)** | 5種 | 10〜15種 | +5〜10 | 新しい敵タイプに対応する種族。固有効果・給与・バフプールの差別化。CSV化推奨 |
| B3 | **スタッフバフ (StaffBuffData)** | 8種 | 15〜20種 | +7〜12 | Fish/Dessert のカテゴリ特化、ドロップ率UP、客数UPなど。CSV化推奨 |
| B4 | **カレンダーイベント (CalendarEventData)** | 3種 | 10〜15種 | +7〜12 | 各カテゴリのボーナスデー、全体セール、レアドロップUP日など。CSV化推奨 |
| B5 | **素材 (IngredientData)** | 12種 | 15〜20種 | +3〜8 | 高レアリティ素材（★4〜5）の追加。後半マップの敵ドロップ用 |
| B6 | **料理 + レシピ** | 30品 | 40〜50品 | +10〜20 | 新素材を使った上位レシピ。ChefLv4〜5帯の充実。CSV/JSON追記 |

### 優先度 C — 演出・没入感のために必要

| # | データ種別 | 現状 | 必要なもの | 作業内容 |
|---|-----------|------|-----------|---------|
| C1 | **BGM オーディオ** | 0曲 | 5〜8曲 | タイトル / 拠点 / フィールド / 通常戦闘 / ボス戦 / 経営 / リザルト / ゲームオーバー |
| C2 | **SE オーディオ** | 0個 | 20〜30個 | 斬撃 / ヒット / ジャスト成功 / ガード / メニュー決定 / メニューカーソル / 料理完成 / 客入店 / 金銭音 / レベルアップ 等 |
| C3 | **キャラクターモデル/スプライト** | カプセル | 最低限の3Dモデル or 2Dスプライト | プレイヤー・敵（差し替え用。現在はプリミティブ） |
| C4 | **UI テクスチャ/アイコン** | 0個 | 30〜50個 | 素材アイコン / 料理アイコン / 属性アイコン / ステータスアイコン / バフアイコン |
| C5 | **UXML / USS** | 0個 | 2〜4ファイル | BattleUI.uxml / BattlePanelSettings.asset（現在プロシージャルUI） |
| C6 | **アニメーション** | 0個 | 基本セット | Idle / Attack / Hit / Guard / Skill / Victory / Defeat |

---

## 6. CSV 化ロードマップ（推奨）

現在 SampleDataGenerator にハードコードされている残りのデータも
CSV / JSON に移行すると拡張性が飛躍的に向上する。

| データ | 現在の管理方法 | CSV化の優先度 | ファイル名案 |
|--------|---------------|-------------|-------------|
| StaffBuffData | SampleDataGenerator ハードコード | 高 | `staff_buffs.csv` |
| StaffRaceData | SampleDataGenerator ハードコード | 高 | `staff_races.csv` + `staff_race_buffs.json` |
| CalendarEventData | SampleDataGenerator ハードコード | 中 | `calendar_events.csv` |
| EnemyData | DevilsDinerSetupTool + FieldSceneAutoBuilder | 高 | `enemies.csv` |
| CharacterStats | DevilsDinerSetupTool ハードコード | 高 | `characters.csv` |
| WeaponData | DevilsDinerSetupTool ハードコード | 中 | `weapons.csv` |
| MapData | FieldSceneAutoBuilder ハードコード | 低 | `maps.csv` |

移行の手順:
1. CSV/JSON ファイル作成（`Assets/MasterData/` 配下）
2. `MasterDataImporter.cs` にインポート関数を追加
3. `SampleDataGenerator.cs` / 各 AutoBuilder からハードコードを削除
4. Unity エディタで `Import Master Data` → `Generate Sample Data` を実行

---

## 7. 推奨される次のステップ

### Step 1: Unity エディタ操作（即実行可能）
```
1. DevilsDiner > Generate Sample Staff & Calendar Data
2. DevilsDiner > Auto Setup Boot Scene
3. DevilsDiner > Auto Setup Battle Scene
4. DevilsDiner > Auto Setup Base Scene
5. DevilsDiner > Auto Setup Field Scene  → NavMesh ベイク
6. DevilsDiner > Auto Setup Management Scene
```

### Step 2: 通しプレイテスト
BootScene → BaseScene → FieldScene → BattleScene → ManagementScene → BaseScene
の1サイクルが正常動作するか確認。

### Step 3: 敵データ拡充（A1 + A2）
ゲームプレイのボリュームに直結する最重要コンテンツ。
マップ5種 × 4〜6体 = 20〜30体の敵を定義。

### Step 4: コンテンツ拡充（B1〜B6）
家具・スタッフ・カレンダーイベントを増やし、
経営パートのリプレイ性を高める。

### Step 5: アセット制作（C1〜C6）
BGM/SE/モデル/アイコンの導入でゲーム体験を完成させる。

---

## 8. データ型 継承ツリー

```
ScriptableObject
├── ItemData (抽象基底)
│   ├── DishData          ← dishes.csv
│   ├── IngredientData    ← ingredients.csv
│   └── WeaponData        ← 今後 CSV 化推奨
├── CharacterStats        ← 今後 CSV 化推奨
├── EnemyData             ← 今後 CSV 化推奨
├── RecipeData            ← recipes.json
├── StaffRaceData         ← 今後 CSV 化推奨
├── StaffBuffData         ← 今後 CSV 化推奨
├── FurnitureData         ← furniture.csv
├── MapData               ← 今後 CSV 化可能
├── CalendarEventData     ← 今後 CSV 化推奨
└── QualityScaleTable     ← 自動生成（固定値）
```

---

*最終更新: 2026-03-10*
