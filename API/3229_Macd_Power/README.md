# MACD Power Strategy

## Overview
The MACD Power strategy is a multi-timeframe momentum system converted from the original MetaTrader expert advisor. The logic combines a pair of linear weighted moving averages (LWMA) calculated on the primary timeframe, two MACD variations, a higher timeframe momentum filter, and a monthly MACD bias. The strategy attempts to participate in impulsive moves once momentum and higher timeframe trend conditions align.

## Core logic
- **Primary moving averages** – A fast and a slow LWMA of the candle typical price (\((High + Low + Close) / 3\)). The strategy requires the fast average to trade below the slow average before any signal is considered, mirroring the original code that waits for pullbacks inside a dominant bearish slope before entering in the direction of the monthly bias.
- **Dual MACD confirmation** – Two MACD indicators with parameters `(12, 26, 1)` and `(6, 13, 1)` must both be above zero for long trades or below zero for short trades. These values reproduce the MQL expert's `MacdMAIN1` and `MacdMAIN2` conditions that measure short-term acceleration.
- **Momentum filter** – Momentum (length 14) is computed on a higher timeframe derived from the primary candle size (e.g., 15‑minute base -> 1‑hour momentum). The absolute distance from 100 is monitored over the three latest momentum readings; at least one of them must exceed the configured threshold to confirm that price is moving decisively.
- **Monthly MACD bias** – A monthly `(12, 26, 9)` MACD (identical to `MacdMAIN0`/`MacdSIGNAL0` in the EA) must have its main line above the signal line for long trades and below the signal line for shorts. This guards against trading against the dominant macro trend.

## Trade management
- **Entry sizing** – The `OrderVolume` parameter defines the base order size. When a reversal of position is required, the engine automatically adds the magnitude of the opposite position so that the net volume is flipped in a single market order.
- **Take profit / stop loss** – Absolute distances are expressed in instrument points and converted to price using `Security.PriceStep` (with a safe fallback to `1`).
- **Trailing stop** – Once price moves in favour by `TrailingActivationPoints`, the stop tracks the highest (long) or lowest (short) price with an offset defined by `TrailingOffsetPoints`.
- **Break-even** – When price reaches `BreakEvenTriggerPoints`, a synthetic break-even stop is armed at `Entry ± BreakEvenOffsetPoints`. If price retreats back to that level the position is closed.
- **Trade limit** – `MaxTrades` limits the number of position initiations per run; once the threshold is reached, no new entries are issued.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary timeframe for signal generation. | 15-minute candles |
| `FastMaLength` | Length of the fast LWMA (typical price). | 6 |
| `SlowMaLength` | Length of the slow LWMA (typical price). | 85 |
| `MomentumLength` | Momentum lookback on the higher timeframe. | 14 |
| `MomentumBuyThreshold` | Minimum absolute distance from 100 required for bullish momentum. | 0.3 |
| `MomentumSellThreshold` | Minimum absolute distance from 100 required for bearish momentum. | 0.3 |
| `TakeProfitPoints` | Take-profit distance in instrument points. | 50 |
| `StopLossPoints` | Stop-loss distance in instrument points. | 20 |
| `TrailingActivationPoints` | Profit (points) required before trailing activates. | 40 |
| `TrailingOffsetPoints` | Gap (points) between trailing stop and extreme price. | 40 |
| `BreakEvenTriggerPoints` | Profit (points) that enables break-even protection. | 30 |
| `BreakEvenOffsetPoints` | Offset (points) applied when moving the stop to break-even. | 30 |
| `MaxTrades` | Maximum number of trades allowed per session. | 10 |
| `OrderVolume` | Base order volume. | 1 |

## Differences versus the MQL expert
- The strategy uses the StockSharp high-level API (`SubscribeCandles` + `Bind/BindEx`) instead of direct tick polling. Indicator values are processed only after candles are finished.
- Money-based trailing and equity stop blocks from the original code are not ported because account-level money management is normally handled by the StockSharp risk framework. Instead, point-based trailing and break-even remain and can be configured to emulate the EA's behaviour.
- Alerts, notifications, and manual order modification helpers from MQL are omitted; the StockSharp engine handles orders directly via market calls.

## Usage notes
1. Choose the primary timeframe by setting `CandleType`. Higher timeframe momentum and the monthly MACD are derived automatically according to the mapping implemented in `GetMomentumCandleType()`.
2. Align `TakeProfitPoints`, `StopLossPoints`, and the trailing/break-even parameters with the instrument's tick size. The defaults reflect the EA's 5-digit Forex settings but can be adapted for other markets.
3. Monitor the `MaxTrades` counter when running automated backtests; set it to a large number if the original EA's martingale-like stacking behaviour is desired.
4. For visual analysis, enable charting in the GUI – the implementation draws candles and the two LWMA curves by default.

