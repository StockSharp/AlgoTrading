# エキスパート ADC PL ストック戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

The **Expert ADC PL Stoch Strategy** is a candlestick pattern strategy converted from the original MQL5 expert advisor *Expert_ADC_PL_Stoch*. It looks for bullish Piercing Line and bearish Dark Cloud Cover formations on finished candles and confirms the signals with the %D line of a Stochastic Oscillator. The method is trend-following when the market retraces into an established move and requires the oscillator to be in extreme zones before opening positions.ポジションの終了は、極端な領域からの Stochastic クロスオーバーに基づいており、ソース システムの投票ベースの終了ロジックを反映しています。

## 取引ロジック

1. Subscribe to a configurable candle type (default: 1-hour time frame).
2. 終了したローソク足ごとに、ローソク足パターンの評価に必要な最後のローソク足と最近の Stochastic %D 値を維持します。
3. **Long Entry**
   - 前のローソク足のペアは、ピアス ライン パターンを形成する必要があります。
     - Candle at bar *t-1* is bullish with a body greater than the average body size.
     - Candle at bar *t-2* is bearish with a body greater than the average.
     - 強気のローソク足は弱気の安値を下回り、弱気の実体の内側に戻りますが、終値平均によれば全体の傾向は下降傾向にあります。
   - バー *t-1* の Stochastic %D 値は、ロングエントリーしきい値 (デフォルト 30) を下回る必要があります。
4. **ショートエントリー**
   - 前のローソク足のペアは、暗雲カバー パターンを形成する必要があります。
     - Candle at bar *t-2* is bullish with a large body.
     - Candle at bar *t-1* opens above the previous high and closes back within the bullish body.
     - 弱気のローソク足の中間価格は終値の移動平均を上回っており、反転前の上昇トレンドを示しています。
   - バー *t-1* の Stochastic %D は、ショートエントリーしきい値 (デフォルト 70) を超えている必要があります。
5. **Exit Conditions**
   - ロングポジションは、バー *t-1* の Stochastic %D がバー *t-2* と比較して上限 (80) または下限 (20) のしきい値を下回ったときにクローズされます。
   - ショート ポジションは、バー *t-1* の Stochastic %D がバー *t-2* と比較して下限 (20) または上限 (80) のしきい値を超えたときにクローズされます。
6. すべての計算は完成したキャンドルに対して実行されます。イントラバー処理は使用されません。

## パラメーター

| 名前 | 説明 | デフォルト |
| ---- | ----------- | ------- |
| `CandleType` | パターン検出に使用されるローソク足の時間枠。 | 1時間 |
| `StochasticLength` | Stochastic オシレーターの基本長。 | 47 |
| `StochasticKPeriod` | %K ラインのスムージング長。 | 9 |
| `StochasticDPeriod` | %D ラインのスムージング長。 | 13 |
| `StochasticSlow` | Additional slowing factor applied to the oscillator. | 3 |
| `AverageBodyPeriod` | Number of candles used to measure the reference body size and close average. | 5 |
| `LongEntryThreshold` | ロングトレードを開始する前に許可される最大 %D 値。 | 30 |
| `ShortEntryThreshold` | ショートトレードを開始する前に必要な最小 %D 値。 | 70 |
| `ExitLowerThreshold` | Lower boundary used for exit crossovers. | 20 |
| `ExitUpperThreshold` | 出口クロスオーバーに使用される上限境界。 | 80 |

## リスク管理

- この戦略は、基本戦略ボリューム (デフォルトでは 1 契約/ロット) を使用して成行注文を送信します。
- 自動保護命令は設定されていません。必要に応じて、外部リスク管理または `StartProtection` を追加できます。
- Only one position is managed at a time;反対の信号は、新しいポジションを開く前にアクティブなポジションを閉じます。

## 注意事項

- 平均ローソク本体と近似平均は、MQL5 の投票ロジックを厳密に再現するために、過去のローソク足から計算されます。
- Stochastic 値は、元のエキスパートアドバイザーで使用されたのと同じオフセットを評価するために、完成したバーごとに保存されます。
- Trades are opened and closed only when the strategy is fully formed and trading is allowed by the base class checks.
