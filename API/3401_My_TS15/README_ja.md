# 私のTS15移動平均トレーリングストップ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、既存のネット ポジション周辺のトレーリング ストップ注文を管理することにより、元の **my_ts15.mq5** エキスパート アドバイザーの動作を再現します。線形加重移動平均 (LWMA) はストップの配置を制御しますが、他の平滑化手法で置き換えることもできます。 The logic continuously:

* 設定可能な数の完了したローソク足から移動平均値を読み取ります。
* Compares price progress with the moving average trail and price-based offsets.
* Moves the protective stop order only when the new level improves the previous one by at least the specified step.
* オプションで、ストップをクランプするか、制限が破られたときにポジションを即座に清算することにより、最大損失距離を強制します。

この戦略はエントリーシグナルを生成しません。これは、同じ証券でポジションをオープンする他のコンポーネント (手動または自動) と一緒に実行することを目的としています。

## 取引ロジック

1. Subscribe to the selected candle series and bind a moving average indicator using the StockSharp high-level API.
2. ローソク足が終了したらすぐにインジケーターの結果を保存し、現在のバーから `MaBarsTrail + MaShift` バー後の値を取得します。
3. 商品ティックサイズを使用して、ポイントベースの設定を絶対価格距離に変換します。
4. For long positions, choose the lowest of:
   * 移動平均からそのオフセットを引いたもの。
   * 現在の価格から「利益中」のオフセットを差し引いたもの。
Afterwards clamp the trail to the “in loss” distance and optionally to the maximum allowed loss.
5. ショートポジションの場合は、次の中で最も高いものを選択します。
   * 移動平均とそのオフセット。
   * The current price plus the “in profit” offset.
Afterwards clamp the trail to the “in loss” distance and optionally to the maximum allowed loss.
6. Update the stop order only when the improvement exceeds `TrailStepPoints` (unless it is zero, in which case every improvement is accepted).
7. 価格が最大損失距離に違反し、`EnforceMaxStopLoss` が有効になっている場合、ストラテジーはポジションを即座にクローズします。

All price inputs use the candle price specified in `MaPrice`, matching the original MQL setting where the indicator is fed with the `PRICE_WEIGHTED` series.

## パラメーター

| 名前 | デフォルト | 説明 |
| ---- | ------- | ----------- |
| `MaPeriod` | `50` | トレーリングバックボーンとして使用される移動平均の長さ。 |
| `MaShift` | `0` | 移動平均値をサンプリングするときに適用される追加のシフト (バー単位)。 |
| `MaMethod` | `LinearWeighted` | 移動平均の平滑化方法 (単純、指数関数、平滑化、線形加重)。 |
| `MaPrice` | `Weighted` | 移動平均にフィードされたローソク足の価格。 |
| `MaBarsTrail` | `1` | Number of completed bars between the current candle and the moving average sample. |
| `TrailBehindMaPoints` | `5` | Distance in points kept between the stop and the moving average. |
| `TrailBehindPricePoints` | `30` | ポジションが利益を生む場合に価格から維持されるポイント単位の距離。 |
| `TrailBehindNegativePoints` | `60` | Distance in points kept behind the price when the position is losing. |
| `TrailStepPoints` | `0` | ストップを移動する前に必要な最小限の改善 (ポイント単位)。 Zero は「常に更新」の動作を再現します。 |
| `EnforceMaxStopLoss` | `false` | 有効にすると、ストップを最大許容損失に固定し、価格がその制限を超えたときにポジションを清算します。 |
| `MaxStopLossPoints` | `100` | 最大許容損失距離 (ポイント単位)。 |
| `ShowIndicator` | `true` | UI が使用可能な場合は、チャート上に移動平均と取引マーカーを描画します。 |
| `CandleType` | `M1` | 計算を行うキャンドルのデータ型。 |

すべてのポイントベースの入力は、`Security.PriceStep` から計算された商品のピップサイズを介して価格距離に変換されます。

## 変換メモ

* MQL エキスパートが MA ハンドルを手動で更新しました。 The StockSharp implementation uses `BindEx` to process the indicator without accessing internal buffers or calling `GetValue`.
* 買い/売り価格は完成したローソク足から直接取得できないため、末尾の計算では `MaPrice` によって選択されたローソク足価格が使用されます。元のスクリプトは同じ加重価格をインジケーターに供給し、それを買値/売値ティックと比較したため、これにより動作の一貫性が保たれます。
* `PositionModify` は、保護的ストップ注文のキャンセルと再作成に置き換えられます（長い場合は `SellStop`、短い場合は `BuyStop`）。 The strategy stores the last stop level to mimic the MetaTrader trailing thresholds.
* オプションの強制決済 (`pre_init`) は元のロジックに従います。市場が `MaxStopLossPoints` を超えると、ポジションは直ちに決済されます。
* No entry logic has been added;ユーザーは、この後続モジュールを独自のシグナルプロバイダーと組み合わせる必要があります。

## 使用のヒント

1. Attach the strategy to the same security that opens the positions.
2. ポイントの距離を金融商品のティック サイズに調整します (外国為替シンボルでは通常「ピップ」値が使用されますが、CFD では異なる乗数が必要になる場合があります)。
3. Set `TrailStepPoints` to a positive value to reduce order churn on illiquid instruments.
4. 別のリスク マネージャーがすでにハード ストップ距離を制御している場合は、`EnforceMaxStopLoss` を無効にします。
5. 移動平均とトレーリングの動作を視覚化するためにパラメータを調整する間は、`ShowIndicator` を有効にしておきます。
