# Cyclops サイクル識別子戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、MetaTrader エキスパート アドバイザー **Cyclops v1.2** を独自の *CycleIdentifier* インジケーターとともに StockSharp の高レベル API に移植します。このアルゴリズムは、平滑移動平均 (SMMA) を使用して終値を平滑化し、長いルックバック平均の真の範囲を通じて最近のボラティリティを測定し、価格が最新の変動から十分に離れた場合にサイクルの転換点をマークします。メジャーサイクルの反転は新しいエントリーを生成しますが、マイナー反転はオプションの終了シグナルを提供します。

構成可能なゼロラグ フィルターは、平滑化された系列の傾きを検証します。このフィルターは、平滑化された価格データ、または同じシリーズから派生したワイルダー スタイルの RSI に対して直接機能します。追加の確認は古典的なモメンタムインジケーターを通じて利用でき、取引は特定の平日/時間枠に制限できます。

## 信号ロジック

- **サイクル検出** – 内部ステートマシンは、平滑化された価格の最後のスイング高値と安値を追跡します。価格が適応しきい値 (平均レンジ × *長さ*) を超えた場合、戦略はマイナーサイクルをマークします。メジャー サイクルにフラグを立てるには、より大きな倍数 (*MajorCycleStrength*) が必要です。
- **エントリー** – 主要な強気サイクル (`MajorBuy`) はロングで始まります。主要な弱気サイクル（`MajorSell`）オープンショート。アクティブなポジションは、反対側に反転する前に自動的にクローズされます。
- **オプションのエグジット** – *UseExitSignal* が有効になっている場合、反対のメジャー サイクルが存在しない場合、収益性の高い取引は、対応するマイナー サイクル シグナル (ロングの場合は `MinorSellExit`、ショートの場合は `MinorBuyExit`) で終了できます。
- **ゼロラグ フィルター** – *UseCycleFilter* が有効な場合、ゼロラグ平滑化フィルターは傾き (ロングの場合は上昇、ショートの場合は下降) を確認する必要があります。フィルター ソースは *CycleFilterMode* (平滑化価格または RSI) によって選択されます。
- **モメンタム フィルター** – *UseMomentumFilter* を有効にすると、エントリーにはロングの場合は `Momentum ≥ MomentumTriggerLong`、ショートの場合は `Momentum ≤ MomentumTriggerShort` が必要になります。

## 貿易管理

- **固定ターゲット** – *TakeProfitPips* および *StopLossPips* は、商品ピップのオプションの固定出口を定義します。
- **損益分岐点** – *BreakEvenTrigger* ピップの利益に達すると、ストップはエントリー ± 1 ピップに引かれます。
- **Trailing** – *TrailingStopTrigger* activates a trailing stop that follows price at *TrailingStopPips* once the trigger distance is achieved.
- **セッション制御** – *UseTimeRestriction* が true の場合、新しいポジションは、その日の `DayEnd` (0=日曜日) より前から `HourEnd` (両端を含む) までのみ許可されます。既存の取引はその後も管理されます。

## パラメーター

| パラメータ | 説明 |
|-----------|-------------|
| `Volume` | エントリーに使用される注文量。 |
| `PriceActionFilter` | Length of the smoothed moving average applied to close price. |
| `Length` | マイナーサイクルを検出するために平均範囲に適用される乗数。 |
| `MajorCycleStrength` | メジャースイングとマイナースイングを分ける乗数。 |
| `UseCycleFilter` | ゼロラグスロープの確認を有効にします。 |
| `CycleFilterMode` | Selects zero-lag input: smoothed price (`Sma`) or RSI (`Rsi`). |
| `FilterStrengthSma` | 平滑化された価格が使用される場合のゼロラグ フィルターの長さ。 |
| `FilterStrengthRsi` | Length and RSI period when the filter relies on RSI values. |
| `UseMomentumFilter` | 勢いの確認をオンまたはオフにします。 |
| `MomentumPeriod` | モメンタムインジケーターの長さ。 |
| `MomentumTriggerLong` | ロングエントリーに必要な最小限の勢い。 |
| `MomentumTriggerShort` | ショートエントリーで許容される最大の勢い。 |
| `UseExitSignal` | 利益が出た場合、マイナーサイクルベースのエグジットが可能になります。 |
| `UseTimeRestriction` | Limits trading to the configured weekday/hour window. |
| `DayEnd` | 新しいエントリが許可される週の最終日。 |
| `HourEnd` | Last hour on the final trading day for new entries. |
| `BreakEvenTrigger` | Profit in pips required to activate the break-even stop. |
| `TrailingStopTrigger` | トレーリングを開始するために必要な利益 (pips)。 |
| `TrailingStopPips` | Distance in pips maintained by the trailing stop. |
| `TakeProfitPips` | 利益確定距離をピップ単位で修正しました。 |
| `StopLossPips` | ストップロス距離をピップ単位で修正しました。 |
| `CandleType` | 戦略に影響を与える主要な時間枠。 |

## オリジナルの EA との違い

- 平均範囲は、250 期間の平均真の範囲に *Length* を乗じて推定され、MQL で使用されるローリング高値/低値スパンと同等の動作を提供します。
- モメンタムの確認では、実際のインジケーター値が使用されます (MQL スクリプトがピップ乗数 `bm` と比較され、実質的にフィルターが無効になります)。
- Zero-lag smoothing is implemented with the same recursive coefficients but expressed in decimal arithmetic. RSI mode uses a Wilder RSI whose period equals *FilterStrengthRsi*.

## 使用上の注意

1. Select the instrument and bind the `CandleType` parameter to the desired timeframe.
2. Configure the risk and session settings to match your broker environment.
3. Enable *UseCycleFilter* or *UseMomentumFilter* when a stricter confirmation is required;エントリを高速化するには無効にしますが、ノイズが多くなります。
4. The strategy maintains at most one open position. Opposite cycle signals close the current position before a new one is evaluated.
