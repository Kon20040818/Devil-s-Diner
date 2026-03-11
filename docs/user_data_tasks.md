# Devil's Diner — ユーザー作業タスク一覧

## Priority A: ゲームを通しで動かすための必須データ

---

### A-1. 敵データ拡充（現在5体 → 目標20体）

**対象ファイル**: `Assets/MasterData/enemies.csv` + `Assets/MasterData/characters.csv`

現在5体（Cactus, Boss, DragonLord, PoisonHydra, Dummy）のみ。
20体程度に拡充すると、フィールド・バトル・スカウトが一通り機能する。

#### enemies.csv カラム仕様

| 列名 | 型 | 説明 | 例 |
|---|---|---|---|
| `Id` | string | `ENM_` + 名前 | `ENM_FireDrake` |
| `EnemyName` | string | 表示名 | `炎竜` |
| `MaxHP` | int | HP | `800` |
| `BaseAttack` | int | 攻撃力 | `45` |
| `DropItemNormal` | string | 通常ドロップ食材ID | `ING_Meat` |
| `DropItemJust` | string | ジャストドロップ食材ID | `ING_Scale` |
| `DropRateNormal` | float | 通常ドロップ率 (0.0〜1.0) | `0.8` |
| `DropRateJust` | float | ジャストドロップ率 (0.0〜1.0) | `1.0` |
| `GoldReward` | int | 撃破報酬Gold | `120` |
| `StaffRace` | string | 紐づく種族ID | `RACE_FireDrake` |
| `StatsId` | string | 紐づくCharacterStats ID | `STAT_FireDrake` |

#### characters.csv カラム仕様（敵行を追加）

ヒーロー行 (`STAT_Hero`) の下に、敵ごとの行を追記する。

| 列名 | 型 | 説明 | 例 |
|---|---|---|---|
| `Id` | string | `STAT_` + 名前（enemies.csv の StatsId と一致） | `STAT_FireDrake` |
| `DisplayName` | string | 表示名 | `炎竜` |
| `Element` | string | `Physical/Fire/Ice/Lightning/Wind/Dark` | `Fire` |
| `MaxHP` | int | HP | `800` |
| `Attack` | int | 攻撃力 | `45` |
| `Defense` | int | 防御力 | `15` |
| `Speed` | int | 速度（大きいほど先に行動） | `90` |
| `MaxEP` | int | EP最大値 | `100` |
| `EPGainOnAttack` | int | 通常攻撃時EP獲得 | `15` |
| `EPGainOnHit` | int | 被ダメ時EP獲得 | `10` |
| `EPGainOnSkill` | int | スキル使用時EP獲得 | `25` |
| `SkillMultiplier` | float | スキルダメージ倍率 | `1.5` |
| `UltimateMultiplier` | float | 必殺技ダメージ倍率 | `3.0` |
| `SkillTargetMode` | string | `Single` or `AllEnemies` | `Single` |
| `MaxToughness` | int | 靭性値（0=靭性無効） | `60` |
| `WeakElements` | string | 弱点属性（セミコロン区切り） | `Ice;Wind` |
| `BaseActionValue` | float | 行動値（大きいほど遅い） | `10000` |
| `PhysicalRes` | float | 物理耐性 (0=等倍, -0.5=弱点, 0.5=耐性) | `0` |
| `FireRes` | float | 火耐性 | `0.5` |
| `IceRes` | float | 氷耐性 | `-0.5` |
| `LightningRes` | float | 雷耐性 | `0` |
| `WindRes` | float | 風耐性 | `0` |
| `DarkRes` | float | 闇耐性 | `0` |

#### 設計ポイント

- **ドロップ食材**: 敵ごとにドロップする `ING_XXX` を決める → 料理の素材入手経路になる
- **種族紐づけ**: 敵ごとに `RACE_XXX` を紐づける → スカウト時にその種族のスタッフになる
- **弱点属性**: 全属性に散らすと戦略性が出る（Fire敵はIce弱点など）
- **既存食材ID一覧**: `ING_Meat`, `ING_Vegetable`, `ING_Bone`, `ING_Scale`, `ING_Poison`, `ING_Herb`, `ING_Fish`, `ING_Egg`, `ING_Mushroom`, `ING_Crystal`, `ING_Spice`, `ING_Fruit`

---

### A-2. スタッフ種族拡充（現在5 → 目標10〜15）

**対象ファイル**: `Assets/MasterData/staff_races.json`

敵を20体に増やす場合、種族も増やす必要がある。
敵1体 = 1種族が基本だが、同種族に複数の敵を紐づけてもOK。

#### JSON フォーマット

```json
{
  "id": "RACE_XXX",
  "raceName": "表示名",
  "fixedEffect": "QualityUp",
  "fixedEffectValue": 0.15,
  "baseSalary": 60,
  "possibleBuffs": ["SBUF_QualityUp", "SBUF_CookSpeed"],
  "minBuffCount": 1,
  "maxBuffCount": 2
}
```

#### fixedEffect 選択肢

| 値 | 効果 |
|---|---|
| `QualityUp` | 料理品質アップ |
| `SatisfactionUp` | 顧客満足度アップ |
| `CookSpeedUp` | 調理速度アップ |
| `SalaryDiscount` | 給料割引 |
| `DropRateUp` | ドロップ率アップ |

#### 既存バフID一覧（possibleBuffs で使用）

`SBUF_QualityUp`, `SBUF_SatisfactionUp`, `SBUF_FreshnessUp`, `SBUF_CookSpeed`, `SBUF_SalaryDown`, `SBUF_MeatSpecialty`, `SBUF_SaladSpecialty`, `SBUF_SuperQuality`

---

### A-3. マップデータ（新規作成、最低3マップ）

**対象**: `MapData` ScriptableObject — **現在マスターデータファイルが存在しない**

`BaseSceneUI` が `Resources.LoadAll<MapData>("")` で出撃先一覧を読み込むため必須。
CSV インポーターは未実装のため、Unity Inspector で手動作成するか、CSV追加を依頼。

#### 必要フィールド

| フィールド | 型 | 説明 | 例 |
|---|---|---|---|
| `Id` | string | `MAP_` + 名前 | `MAP_Desert` |
| `MapName` | string | 表示名 | `灼熱の荒野` |
| `Description` | string | 説明文 | `サボテン族が棲む砂漠地帯` |
| `Environment` | enum | `Desert/Forest/Swamp/Volcano/Castle` | `Desert` |
| `RequiredShopLevel` | int | 解放条件（店舗レベル） | `1` |
| `RecommendedLevel` | int | 推奨レベル（表示用） | `1` |
| `SceneName` | string | ロードするシーン名 | `FieldScene` |

#### 推奨構成例

| マップ | 環境 | 解放Lv | 出現敵イメージ |
|---|---|---|---|
| 灼熱の荒野 | Desert | 1 | サボテン系、砂蟲系 |
| 深淵の森 | Forest | 2 | 茸系、毒蛇系 |
| 魔王城 | Castle | 3 | ボス系、竜系 |

> **NOTE**: CSVインポーター（`ImportMaps()`）が必要であれば依頼してください。

---

## Priority B: ゲーム体験を充実させるデータ

---

### B-1. 家具データ拡充（現在3 → 目標8〜10）

**対象ファイル**: `Assets/MasterData/furniture.csv`

現在3つ（テーブル/装飾品/厨房改修）のみで、経営パートの選択肢が少ない。

#### カラム仕様

| 列名 | 型 | 説明 | 例 |
|---|---|---|---|
| `Id` | string | `FRN_` + 名前 | `FRN_Chandelier` |
| `DisplayName` | string | 表示名 | `シャンデリア` |
| `Description` | string | 説明文 | `高級感溢れる照明` |
| `Type` | string | `Table/Chair/Decoration/Lighting/Kitchen` | `Lighting` |
| `Price` | int | 購入価格 | `800` |
| `SatisfactionBonus` | float | 満足度ボーナス | `0.25` |
| `CustomerBonus` | int | 来客数ボーナス | `1` |

> `FurnitureData` には `_prefab (GameObject)` フィールドもあるが、CSVからは設定不可。
> 3Dモデルが用意できたら Inspector で手動設定。

---

### B-2. カレンダーイベント拡充（現在3 → 目標7〜10）

**対象ファイル**: `Assets/MasterData/calendar_events.csv`

現在3つ（肉の日/収穫祭/グルメウィーク）。
20日サイクルで遊ぶなら、もっとイベントがあると経営に変化が出る。

#### カラム仕様

| 列名 | 型 | 説明 | 例 |
|---|---|---|---|
| `Id` | string | `CAL_` + 名前 | `CAL_DessertFair` |
| `EventName` | string | 表示名 | `スイーツフェア` |
| `Description` | string | 説明文 | `デザートの売上が大幅アップ！` |
| `TriggerDays` | string | 発生日（セミコロン区切り） | `4;11;18` |
| `BonusCategoryEnabled` | bool | カテゴリボーナス有無 | `true` |
| `BonusCategory` | string | `Meat/Fish/Salad/Dessert` | `Dessert` |
| `SatisfactionMultiplier` | float | 満足度倍率 | `1.6` |
| `FreshnessMultiplier` | float | 鮮度倍率 | `1.2` |

---

### B-3. スタッフバフ拡充（現在8 → 目標12〜15）

**対象ファイル**: `Assets/MasterData/staff_buffs.csv`

種族を増やすなら、バフプールも増やさないとバリエーションが出ない。

#### カラム仕様

| 列名 | 型 | 説明 | 例 |
|---|---|---|---|
| `Id` | string | `SBUF_` + 名前 | `SBUF_FishSpecialty` |
| `DisplayName` | string | 表示名 | `魚料理の達人` |
| `Description` | string | 説明文 | `魚料理の品質にボーナス` |
| `Type` | string | バフ種別（下表参照） | `CategorySpecialty` |
| `Value` | float | 効果値 | `0.25` |
| `TargetCategory` | string | カテゴリ特化用（空欄可） | `Fish` |
| `Rarity` | int | レアリティ (1〜5) | `3` |

#### Type 選択肢

| Type | 説明 |
|---|---|
| `QualityBonus` | 品質加算 |
| `SatisfactionBonus` | 満足度加算 |
| `FreshnessBonus` | 鮮度加算 |
| `CookSpeed` | 調理速度アップ |
| `SalaryReduction` | 給料割引 |
| `CategorySpecialty` | カテゴリ特化（`TargetCategory` 列が必須） |

#### 追加候補例

- `SBUF_FishSpecialty` — 魚料理の達人（CategorySpecialty, Fish）
- `SBUF_DessertSpecialty` — 菓子職人（CategorySpecialty, Dessert）
- `SBUF_SuperSatisfaction` — 超接客（SatisfactionBonus, 0.25, Rarity 5）
- `SBUF_SuperFreshness` — 超鮮度管理（FreshnessBonus, 0.25, Rarity 5）
- `SBUF_SuperCookSpeed` — 神速調理（CookSpeed, 0.35, Rarity 5）

---

## Priority C: ビジュアル・オーディオアセット

---

### C-1. ポートレート / アイコン画像

以下の ScriptableObject が `Sprite` フィールドを持つが、CSV/JSON からは設定不可。
PNG/JPG を `Assets/Textures/` 配下に配置 → Import 後に各 SO の Inspector で手動設定。

| 対象 | フィールド | 用途 |
|---|---|---|
| `CharacterStats` | `_portrait` | バトルUI、タイムライン |
| `StaffRaceData` | `_portrait` | スタッフ管理UI |
| `ItemData`（食材/料理/武器） | `_icon` | インベントリ、ショップ |
| `StaffBuffData` | `_icon` | バフ表示 |
| `CalendarEventData` | `_icon` | カレンダーUI |

---

### C-2. 3Dモデル / プレハブ

| 対象 | 用途 | 現在の状態 |
|---|---|---|
| 敵モデル | フィールドシンボル + バトル表示 | 赤カプセルで仮生成 |
| 家具モデル | 経営パート店内表示 | `FurnitureData._prefab` 未設定 |
| プレイヤーモデル | フィールド操作キャラ | FieldScene Auto Setup で仮生成 |

---

### C-3. Audio（BGM / SE）

| 種別 | 用途 |
|---|---|
| BGM | バトル、フィールド、経営、拠点 |
| SE | 攻撃、ジャスト成功、メニュー操作、料理完成、レベルアップ |

`Assets/Audio/` 配下に配置 → `AudioManager` から参照。

---

## 推奨作業順序

```
1. A-1  敵データ 20体       (enemies.csv + characters.csv)
2. A-2  種族データ 10〜15    (staff_races.json) ← 敵と対応
3. A-3  マップデータ 3〜5    (新規 or Inspector手動)
4. B-3  スタッフバフ 12〜15  (staff_buffs.csv) ← 種族のpossibleBuffsに必要
5. B-1  家具 8〜10           (furniture.csv)
6. B-2  カレンダーイベント    (calendar_events.csv)
7. C-1  ポートレート/アイコン (PNG → Inspector手動設定)
8. C-2  3Dモデル              (FBX/プレハブ → Inspector手動設定)
9. C-3  Audio                 (WAV/OGG → AudioManager)
```

---

## 全データ完了後の Unity エディタ作業

```
1. DevilsDiner > Generate All Master Data    ← 全 ScriptableObject 再生成
2. DevilsDiner > Auto Setup Boot Scene       ← BootScene 生成
3. DevilsDiner > Auto Setup Battle Scene     ← BattleScene 生成
4. DevilsDiner > Auto Setup Base Scene       ← BaseScene 生成
5. DevilsDiner > Auto Setup Field Scene      ← FieldScene 生成 → NavMesh ベイク
6. DevilsDiner > Auto Setup Management Scene ← ManagementScene 生成
7. 通しプレイテスト: Boot → Base → Field → Battle → Management → ループ
```
