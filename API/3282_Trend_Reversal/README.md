# Trend Reversal Strategy

## Overview
The Trend Reversal strategy is a directional system that attempts to capture breakouts after a short-term pullback inside an existing trend. It was ported from the MetaTrader "Trend Reversal" expert advisor and rewritten to use the high-level StockSharp API. The conversion keeps the core confirmation stack (moving averages, momentum, and MACD) while replacing the original graphical line filters with price overlap checks that are easier to reproduce programmatically.

## Indicator Stack
- **Linear Weighted Moving Averages (LWMA)** on typical price with customizable fast and slow lengths. The fast line tracks the latest swing, while the slow line identifies the dominant trend.
- **Momentum oscillator** calculated on the same timeframe. The strategy records the absolute distance from the neutral 100 level for the latest three closed candles to emulate the MetaTrader logic.
- **MACD signal line pair** configured with independent fast, slow, and signal lengths. The histogram direction is used as a higher-timeframe confirmation for both long and short trades.

## Trade Logic
1. Wait for a finished candle on the configured timeframe. The strategy ignores partially formed bars.
2. Ensure that both LWMAs and the momentum indicator are fully formed. Without enough history the system remains flat.
3. Keep a rolling queue of the three most recent momentum deviations from 100. A setup is valid only if at least one of these values exceeds the respective buy or sell threshold.
4. Require that the candle from two bars ago has a lower low than the high of the previous candle. This recreates the "overlapping" structure used in the original EA to detect a tight consolidation before the breakout.
5. Evaluate directional filters:
   - **Long:** fast LWMA above slow LWMA and MACD main value above the signal line.
   - **Short:** fast LWMA below slow LWMA and MACD main value below the signal line.
6. Respect the net position limit. The strategy enters or adds to a position only when the absolute exposure (current position divided by trade volume) is below the configured `MaxPositions` value.
7. Orders are sent with `BuyMarket()` or `SellMarket()` which allows partial or full reversals depending on the current exposure.

## Risk Management
- Optional **take profit** and **stop loss** distances (expressed in price units) can be attached through StockSharp's built-in protective block. Both levels are disabled when a parameter is set to zero.
- No automatic trailing stop or break-even adjustment is included in this port. These features can be implemented with additional event handlers if needed.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary timeframe used to build candles. | 15-minute time frame |
| `FastLength` | Period for the fast LWMA. | 6 |
| `SlowLength` | Period for the slow LWMA. | 85 |
| `MomentumLength` | Period of the momentum oscillator. | 14 |
| `MomentumBuyThreshold` | Minimum absolute momentum deviation (from 100) that validates a long setup. | 0.3 |
| `MomentumSellThreshold` | Minimum absolute momentum deviation (from 100) that validates a short setup. | 0.3 |
| `MacdFastLength` | Fast EMA period used inside the MACD filter. | 12 |
| `MacdSlowLength` | Slow EMA period used inside the MACD filter. | 26 |
| `MacdSignalLength` | Signal EMA period used inside the MACD filter. | 9 |
| `TakeProfit` | Take profit distance in price units. Set to 0 to disable. | 50 |
| `StopLoss` | Stop loss distance in price units. Set to 0 to disable. | 20 |
| `TradeVolume` | Order volume expressed in lots. | 1 |
| `MaxPositions` | Maximum number of trade-volume units allowed in the net position. | 1 |

## Usage Notes
- Attach the strategy to a security with valid step and price information so that protective orders work correctly.
- For multi-directional trading (pyramiding or scaling in), increase `MaxPositions`. The strategy will keep adding positions as long as the filters remain valid and the exposure stays within this limit.
- Backtesting should be performed with the same candle timeframe that the `CandleType` parameter specifies. StockSharp will automatically request the proper data when the strategy starts.
- Because the MetaTrader version depended on hand-drawn trend lines, this rewrite substitutes those checks with a deterministic candle overlap condition. This keeps the behaviour consistent between backtests and live execution.

## Differences Compared to the Original EA
- Trailing stop, break-even moves, and equity-based emergency exits are not implemented to keep the sample focused on core signal generation.
- Money management features such as lot multiplication and Magic Number filtering are not needed in StockSharp and were therefore removed.
- The MACD confirmation uses the same timeframe as the trading candles instead of the original monthly aggregation. You can emulate the multi-timeframe setup by subscribing to a slower candle type and binding the MACD filter to that subscription if desired.

## Optimization Tips
- Optimize the moving average lengths first to match the market's dominant cycle, then fine-tune the momentum thresholds.
- Experiment with wider stop-loss and take-profit distances when trading volatile instruments. Since the logic is trend-following, larger exit buffers often improve profitability.
- Monitor drawdown statistics during optimization runs. Increasing `MaxPositions` can improve responsiveness but also magnifies risk.
