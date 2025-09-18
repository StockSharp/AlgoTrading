# Neural Network Template Strategy

## Overview
This strategy replicates the behaviour of the MQL5 expert advisor template that feeds RSI and MACD features into a neural network. Because StockSharp does not ship with the custom network loader from the original project, the strategy replaces the black-box network with a deterministic scoring model while keeping the same market structure and risk controls. The goal is to capture momentum when both RSI and MACD agree on direction and the projected move is large enough to justify a trade.

## Indicators and data
- **Relative Strength Index (RSI, 12 periods)** calculated on candle close, mirroring the original typical price input.
- **Moving Average Convergence Divergence (MACD 12/48/12)** used as a momentum histogram and confidence proxy.
- **Timeframe** configurable; default is 5-minute candles to match the source expert.

## Trading logic
1. On every finished candle the strategy updates rolling queues of RSI and MACD histogram values with the window controlled by `BarsToPattern`.
2. The RSI deviation from 50 and the MACD histogram deviation from its rolling mean are combined into a confidence score using a hyperbolic tangent to emulate the network's squashing function.
3. If the absolute confidence exceeds `TradeLevel` and the projected move converted to points is beyond `MinTargetPoints`, the strategy issues a market order in the direction suggested by the score.
4. A dynamic take-profit equal to the projected move multiplied by `ProfitMultiply` and capped by `MaxTakeProfitPoints` is stored for manual exit handling. A symmetric stop-loss in points mirrors the original behaviour.
5. While a position is open the strategy checks every finished candle: if price hits the stored stop or target it closes the position at market and resets the internal state.

## Parameters
| Parameter | Description |
| --- | --- |
| `BarsToPattern` | Number of candles stored in the rolling window used to calculate RSI and MACD statistics. |
| `TradeLevel` | Minimum confidence (0-1) required to open a position. |
| `ProfitMultiply` | Multiplier applied to the projected move before capping it with `MaxTakeProfitPoints`. |
| `MinTargetPoints` | Minimum number of price points required from the projection to enter a trade. |
| `MaxTakeProfitPoints` | Maximum distance, in points, allowed for the take-profit. |
| `StopLossPoints` | Distance, in points, of the protective stop relative to the entry price. |
| `TradeVolume` | Volume sent with every market order. |
| `CandleType` | Candle data type or timeframe to subscribe to. |

## Notes
- The confidence model is intentionally deterministic to keep the behaviour transparent while preserving the structure of the original neural-network approach.
- Take-profit and stop-loss levels are managed manually so that each trade keeps its own dynamic targets, similar to how the MQL5 version uses the network output.
- The strategy only evaluates new entries when no position is open, replicating the single-position constraint of the source expert advisor.
