# MartingailExpert v1.0 Stochastic 戦略 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

**MartingailExpert v1.0 Stochastic 戦略** は、MetaTrader 4 エキスパート アドバイザーを直接変換したものです。
`MartingailExpert_v1_0_Stochastic.mq4`。この戦略は、Stochastic オシレーターの %K/%D ラインを監視します。
そして、前に完了したバーが上に勢いを確認したときにポジションをオープンします（ロングの場合）
またはそれ以下 (ショートの場合) 設定可能なしきい値ゾーン。最初の取引が開始されると、アルゴリズムは
追加成行注文のマーチンゲールはしご。量が幾何級数的に増加し、利益確定が共有される
は、最新の追加価格に依然として固定されています。

変換は、StockSharp のハイレベルな API、つまりローソク足のサブスクリプション、インジケーター バインディング、および
組み込みの `BuyMarket`/`SellMarket` ヘルパー。すべてのコードのコメントは英語で書き直され、実装は
プロジェクト ガイドラインで要求されるタブベースのインデント スタイルに従います。

## 取引ロジック

### 1. エントリーシグナル

1. Stochastic オシレーター (`Length = KPeriod`、`%K` スムージング = `Slowing`、`%D` スムージング = `DPeriod`) は
メインのキャンドルサブスクリプションにバインドされています。完成したキャンドルのみが加工されます。
2. この戦略は、以前のバー値を保存することで、元の MQL 呼び出し `iStochastic(..., shift = 1)` を模倣します。
%K と %D の。長いエントリは、`K_prev > D_prev` と `D_prev > ZoneBuy` のときにトリガーされます。短いエントリーは、
`K_prev < D_prev` と `D_prev < ZoneSell` のときにトリガーされます。
3. 最初の取引では、`BuyVolume` または `SellVolume` を使用し、反対方向の状態をリセットして回避します。
長い梯子と短い梯子を混ぜます。

### 2. Martingale の平均化

1. 開いたクラスター (`_buyOrderCount` または `_sellOrderCount` が 0 より大きい) がある場合は常に、戦略
ローソク足の安値（ロングの場合）または高値（ショートの場合）を監視します。
2. **ステップ計算**
   * `StepMode = 0`: 次の追加は、価格がちょうど `StepPoints × PointSize` だけ動くのを待ちます。
最新の約定注文。
   * `StepMode = 1`: 距離は `StepPoints + max(0, 2 × ordersCount − 2)` ポイントとなり、
MQL 式 `step + OrdersTotal*2 - 2`。エクスプレッションにはインストゥルメントのポイント サイズが乗算されます。
(`Security.PriceStep` から派生し、10 進数の 3/5 FX 相場に調整されています)。
3. ローソク足がトリガーレベルに違反した場合、ストラテジーはボリュームが等しい即時成行注文を送信します。
`previousVolume × Multiplier`。音量は楽器の `VolumeStep` に正規化され、上限は
`VolumeMax` (利用可能な場合)、`VolumeMin` を下回る場合はゼロに切り捨てられます。
4. 追加するたびに、共有目標価格は次のように更新されます。
方向に応じて `lastEntryPrice ± ProfitFactorPoints × PointSize × orderCount`。

### 3. 利食い経営

1. ローソク足が共有目標価格（ロングの場合は `High >= target`、
`Low <= target` ショートパンツ用）。追加のチェックでは、加重値を使用して価格距離利益を推定します。
MQL の元の `OrderProfit()` セーフガードを反映した平均エントリー価格。
2. すべてのオープン注文は単一の `SellMarket(Math.Abs(Position))` または
`BuyMarket(Math.Abs(Position))` に電話します。正常に終了すると、内部マーチンゲール状態がリセットされます。
3. 外部環境がポジションをクローズした場合（手動介入、ストップアウト）、次のローソク足は
`Position == 0` はキャッシュされたマーチンゲール状態を自動的にクリアし、戦略の一貫性を保ちます。

### 4. 追加の実装上の注意事項

* ポイント サイズは `Security.PriceStep` から導出されます。 3 桁または 5 桁の 10 進数の FX シンボルの場合、値が乗算されます。
ピップ (`Point`) の MetaTrader の概念をエミュレートするには、10 倍します。
* `StartProtection()` は `OnStarted` で 1 回呼び出されるため、プラットフォームは共通の保護動作を付加できます
(タイムアウト、ハートビートなど)。
* この戦略では、ローソク足、確率指標、および独自の取引を専用のチャート領域に描画して、より簡単に
バックテスト中の目視検査。

## パラメーター

| 名前 | 種類 | デフォルト | 説明 |
| ---- | ---- | ------- | ----------- |
| `StepPoints` | 10進数 | `25` | 別のマーチンゲール注文が行われるまでのポイント単位の距離。 |
| `StepMode` | 整数 | `0` | `0` – 固定距離、`1` – 固定プラス `2 × ordersCount − 2` ポイント。 |
| `ProfitFactorPoints` | 10進数 | `10` | クラスターのテイクプロフィットを計算するためにオープン注文ごとに追加 (または減算) されるポイント。 |
| `Multiplier` | 10進数 | `1.5` | 次の追加のために最後の注文量に適用される乗数。 |
| `BuyVolume` | 10進数 | `0.01` | 最初のロング注文のボリューム。 |
| `SellVolume` | 10進数 | `0.01` | 最初の空売り注文のボリューム。 |
| `KPeriod` | 整数 | `200` | 確率的オシレーターのルックバック期間。 |
| `DPeriod` | 整数 | `20` | %D 信号線の平滑化期間。 |
| `Slowing` | 整数 | `20` | %K (MetaTrader の `slowing`) に追加の平滑化が適用されました。 |
| `ZoneBuy` | 10進数 | `50` | 長いエントリを許可するには最小 %D 値が必要です。 |
| `ZoneSell` | 10進数 | `50` | 短いエントリを許可するには最大 %D 値が必要です。 |
| `CandleType` | `DataType` | `5m time frame` | すべてのインジケーターの計算に使用されるローソク足のタイプ。 |

## フォルダー構造

「」
API/3991/
§── CS/
│ └── MartingailExpertV10StochasticStrategy.cs
§── README.md
§── README_zh.md
━── README_ru.md
「」

Python の実装は、タスクの要件に従って意図的に省略されています。
