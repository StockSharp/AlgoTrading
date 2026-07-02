# 素晴らしいFXトレーダー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

This strategy reproduces the MetaTrader setup from `MQL/8539`, which consists of the custom indicators **AwesomeFxTradera.mq4** and **t_ma.mq4**. The original code paints the Bill Williams Awesome Oscillator histogram in green or red depending on whether the value is rising or falling, and overlays a 34-period linear weighted moving average (LWMA) alongside a smoothed clone of the same curve. StockSharp ポートは同じ計算を維持し、インジケーターの色を取引シグナルに変換します。

## 元の MQL ロジック

1. **AwesomeFxTradera.mq4** computes two exponential moving averages applied to the **open price** with periods 8 and 13. Their difference is stored in `ExtBuffer0`.現在の値が前のバーよりも高い場合はバッファーが緑色にペイントされ、低い場合は赤色でペイントされます。これにより、運動量の符号だけでなく方向も効果的にエンコードされます。
2. **t_ma.mq4** は、始値の 34 期間 LWMA (`ExtMapBuffer1`) とその LWMA の 6 期間単純移動平均 (`ExtMapBuffer2`) をプロットします。スムーズでは、トレンド平均が加速するか減速するかを追跡します。

The MetaTrader chart therefore highlights bullish momentum when the oscillator is above zero and keeps increasing while price trades above the smoothed LWMA.弱気の勢いはその逆の構成です。

## StockSharp の実装

The `AwesomeFxTraderStrategy` subscribes to a configurable candle type (default **M15**) and feeds the indicators with the candle open price to match the MetaTrader buffers.

1. 高速 EMA と低速 EMA は、完成したローソクごとに再計算されます。それらの差により、振動するヒストグラムが再現されます。
2. LWMA は 34 バーのトレンドを追跡し、6 バーの SMA がそれを平滑化します。両方の系列を比較すると、トレンド曲線が上昇しているか下降しているかがわかります。
3. The oscillator colour is rebuilt by comparing the current histogram value with the previous bar, following the `bool up` logic from the MQL implementation.
4. **エントリールール**:
   - オシレーターが正で上昇しており (緑色のバッファー)、LWMA がそのスムーザーを上回っているときにロングを入力します。
   - オシレーターが負で立ち下がり（赤色のバッファー）、LWMA がそのスムーザーを下回っている場合は、ショートを入力します。
5. **終了/反転ルール**: 反対のシグナルによりポジションが反転します。 The order size is automatically increased by the absolute current position so that shorts are closed before a long is established and vice versa.

ソース コードには追加のストップロスまたはテイクプロフィット レベルが定義されていないため、ポートはエグジットのモメンタム フリップのみに依存します。ロギングステートメントには、インジケーターの読み取り値とともに各取引トリガーが記録されます。

## パラメーター

| 名前 | デフォルト | 説明 |
| --- | --- | --- |
| `FastEmaPeriod` | 8 | 発振器レプリカで使用される高速 EMA の長さ。 |
| `SlowEmaPeriod` | 13 | 低速の EMA の長さ。 |
| `TrendLwmaPeriod` | 34 | `t_ma.mq4` から取得された LWMA トレンド フィルターの期間。 |
| `TrendSmoothingPeriod` | 6 | LWMA 値に適用される SMA のウィンドウ。 |
| `CandleType` | 15分の時間枠 | 勢いとトレンドの計算の両方に使用されるローソク足のデータ型。 |

`StrategyParam` メタデータのおかげで、すべてのパラメータは StockSharp UI を通じて最適化できます。

## ファイルマッピング

| MetaTrader ファイル | StockSharpの相手方 | 注意事項 |
| --- | --- | --- |
| `MQL/8539/AwesomeFxTradera.mq4` | `CS/AwesomeFxTraderStrategy.cs` | EMA-on-open オシレーターとその立ち上がり/立ち下がりカラー ロジックを再作成します。 |
| `MQL/8539/t_ma.mq4` | `CS/AwesomeFxTraderStrategy.cs` | トレンド検出のために 6 期間の SMA スムーザーを備えた 34 期間 LWMA を実装します。 |

Python のバージョンは、要求に応じて意図的に省略されています。
