# Turbo Scaler グリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Turbo Scaler Grid 戦略は、MQL5「Turbo Scaler Grid Pending」エキスパート アドバイザーの高レベルの StockSharp 実装です。この戦略は、事前定義された価格レベル付近で保留中のストップ グリッドを管理し、損益分岐点およびトレーリング ロジックでオープン ポジションを動的に保護し、損益のしきい値に達したときにポジションを決済するために口座資本を監視することに重点を置いています。

このロジックは複数のタイムフレームで同時に動作します。

- 設定可能なトリガー タイムフレームは、保留中のグリッドをアクティブにする価格近接シグナルを監視します。
- 追加の 30 分足、2 時間足、および日足ローソク足は、オプションの条件付きトリガーの確認を提供します。
- レベル 1 データは、未決注文を配置し、トレーリングストップを管理するために使用される最新の買値/売値を提供します。

## 取引ルール
1. **保留中のグリッド**
   - 買いストップ注文と売りストップ注文は、設定可能なアンカー価格 (`BuyStopEntry` および `SellStopEntry`) から発注されます。
   - オーダーの間隔は `PendingStepPoints` で、制限は `PendingQuantity` です。
   - 価格トリガーは、トリガータイムフレームの最近のローソク足をチェックして、価格が十分な勢いでアンカーレベルに近づいていることを確認します。
   - 条件トリガーは、保留中の注文を出す前に、追加のマルチタイムフレーム フィルター (日次ブロック範囲、H2 および M30 ローソク足の方向、ミッドレンジ レベル) を検証します。
2. **ポジション保護**
   - 初期ストップロスは、`StopLossPoints` (または固定価格オーバーライド) から計算されます。
   - 価格が `BreakevenTriggerPoints` だけ進むと、ストップはエントリー価格に `BreakevenOffsetPoints` を加えた値 (ロングの場合)、またはオフセットを引いた値 (ショートの場合) に移動します。
   - トレーリングストップは損益分岐点に達した後にのみ有効になり、価格が前のストップを `TrailMultiplier * TrailPoints` 超えると更新されます。
3. **株式の監督**
   - この戦略は変動損益を監視し、ドローダウンが `MaxFloatLoss` (選択した注文量に合わせて調整) を超えた場合にポジションを強制的に清算します。
   - 変動利益トリガーは、内部資本ラインを `EquityBreakeven` に設定し、利益が `EquityTrigger` を超えると、その後に `EquityTrail` が続くことで利益を固定します。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `StopLossPoints` | ポイント単位の初期ストップロス距離。 |
| `BreakevenTriggerPoints` | 損益分岐点移動を有効にするために必要なポイント。 |
| `BreakevenOffsetPoints` | ストップが損益分岐点に移動するときに、オフセットがエントリー価格に追加されます。 |
| `TrailPoints` | 損益分岐点後のトレーリングに使用される距離。 |
| `TrailMultiplier` | 新しいトレーリングストップが設定される前に適用される乗数。 |
| `BuyStopLossPrice` / `SellStopLossPrice` | オプションのロング/ショートポジションの固定ストップ価格。 |
| `BuyStopEntry` / `SellStopEntry` | 保留中のストップグリッドの基本価格。 |
| `OrderVolume` | 未決注文ごとのボリューム。 |
| `PendingQuantity` | アクティブな未決注文の最大数。 |
| `PendingStepPoints` | 連続する未決注文間の距離。 |
| `TriggerCandleType` | 価格トリガーロジックに使用されるローソク足シリーズ。 |
| `PendingPriceTrigger` | 価格近接トリガーを有効にします。 |
| `PendingConditionTrigger` | マルチタイムフレーム確認トリガーを有効にします。 |
| `OrderBuyBlockStart` / `OrderBuyBlockEnd` | 毎日の低ブロックは、長いセットアップを検証するために使用されます。 |
| `OrderSellBlockStart` / `OrderSellBlockEnd` | 短いセットアップを検証するために使用される毎日の高ブロック。 |
| `MaxFloatLoss` | 最大許容浮遊損失（体積によってスケール）。 |
| `EquityBreakeven` | 利益トリガーがアクティブになった後も資産レベルが維持されます。 |
| `EquityTrigger` | 株式ロックを作成するために必要な変動利益。 |
| `EquityTrail` | エクイティロックに適用されるトレーリング距離。 |

## 注意事項
- 注文量は、元の EA の動作に合わせて調整されます (`0.01` ロットは基本ステップとして扱われます)。
- コード内のすべてのコメントは英語で書かれていますが、このドキュメントには迅速なオンボーディングのために詳細な説明が記載されています。
- この戦略では、プロジェクトの要件に従って、高レベルの StockSharp API (`SubscribeCandles`、`Bind`、`BuyStop`、`SellStop`、`SellMarket`、`BuyMarket`) のみを使用します。
