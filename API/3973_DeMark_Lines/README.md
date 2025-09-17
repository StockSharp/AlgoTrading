# DeMark Lines Strategy

## Overview
The DeMark Lines Strategy is a conversion of the MetaTrader "DeMark_lines" indicator (MQL/8296). The original script drew DeMark trendlines based on recent swing highs and lows and highlighted breakouts with optional alerts. This StockSharp implementation transforms the visualization logic into an automated breakout strategy. It continuously scans for downtrend and uptrend lines formed by validated pivot points and opens positions when price action decisively breaks those lines.

## Trading Logic
1. **Pivot detection** – finished candles are processed in chronological order. A candle becomes a swing high when its high is strictly higher than the previous *PivotDepth* candles and not lower than the following *PivotDepth* candles. Swing lows follow the mirrored condition for lows.
2. **Trendline construction** – the two most recent swing highs form the active downtrend resistance line. The two latest swing lows form the uptrend support line. Additional pivots are ignored if they occur too close to the previous anchor, preventing unstable lines.
3. **Breakout filters** – the strategy measures the theoretical trendline value for the current bar index. A breakout requires the closing price to exceed the resistance line (or drop below support) by at least *BreakoutBuffer* pips before trades are executed.
4. **Order placement** – when a bullish breakout appears, any short exposure is closed and a long position of the configured strategy volume is opened. The bearish breakout logic mirrors this behaviour. Each line can trigger a new signal only after a fresh pivot redefines it, avoiding repeated entries while price hovers around the level.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `PivotDepth` | Number of candles on each side required to confirm a pivot high/low. Controls the strictness of swing detection. | 2 |
| `MinBarsBetweenPivots` | Minimum distance, in bars, between two pivots of the same type. Prevents overlapping anchors and keeps trendlines stable. | 5 |
| `BreakoutBuffer` | Extra distance (in pips) added beyond the trendline before a breakout is considered valid. Filters noisy touches. | 2 |
| `CandleType` | Candle data type (timeframe) used for analysis and signal generation. | 30-minute candles |

## Conversion Notes
- Visual objects, alerts, and e-mail notifications from the original indicator are not replicated. Instead, chart areas display price series and the strategy's own trades.
- The strategy relies on StockSharp's high-level candle subscription API and uses internal buffers to validate pivots without referencing indicator history methods forbidden by the guidelines.
- Breakout trades respect the base `Volume` property and automatically reverse existing exposure when the opposite breakout is triggered.

## Usage Tips
- Increase `PivotDepth` on higher timeframes to require broader swings, which reduces signal frequency but improves trendline reliability.
- Adjust `BreakoutBuffer` to account for instrument volatility. Tight values favour earlier entries, while larger buffers help avoid fakeouts.
- Combine the strategy with external money management or protective modules if automated exit handling (take-profit/stop-loss) is required, as the original script only focused on breakout detection.
