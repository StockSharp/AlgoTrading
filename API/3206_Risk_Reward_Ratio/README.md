# Risk Reward Ratio Strategy

## Overview
The **Risk Reward Ratio Strategy** is a high-level StockSharp port of the MetaTrader expert "Risk Reward Ratio". The strategy combines several momentum and trend confirmation filters with a disciplined risk-management module. Entries are generated from a confluence of stochastic oscillators, a linear weighted moving average (LWMA) crossover, a 14-period RSI filter, and a MACD trend check. Risk control is achieved through a pip-based stop-loss, an automatic reward-ratio take-profit, optional trailing stops and break-even logic, and an emergency flat switch that immediately liquidates the position.

The conversion keeps the original spirit of the MetaTrader expert while using StockSharp's candle subscriptions and indicator binding APIs. All indicator processing happens on finished candles and avoids direct access to indicator buffers, preserving the engine's streaming paradigm.

## Trading Logic
1. **Stochastic confluence**
   * A *fast* stochastic (5, 2, 2) delivers the primary momentum signal using the %K line.
   * A *slow* stochastic (21, 10, 4) supplies the directional bias through its smoothed %D line.
   * Long setups require the fast %K to sit above the slow %D, while short setups require the opposite.
2. **RSI confirmation**
   * A 14-period RSI must be above 50 for long trades and below 50 for short trades, ensuring the market is aligned with the proposed direction.
3. **Trend filter via LWMAs**
   * Two linear weighted moving averages (lengths 6 and 85) must confirm the direction: the fast LWMA above the slow LWMA for longs, and below it for shorts.
4. **MACD trend qualifier**
   * The MACD histogram (12, 26, 9) needs to be in agreement with the signal direction. The main line must lead the signal line while staying on the appropriate side of zero.
5. **Momentum deviation filter**
   * A 14-period momentum indicator measures the distance from 100. At least one of the last three momentum readings must exceed the configurable threshold to prove that price is accelerating enough to justify a trade.
6. **Position limits**
   * Net exposure is capped by `MaxPositions * TradeVolume` so the strategy cannot pyramid beyond the original EA's constraint.

Orders are sent as market executions using `BuyMarket` and `SellMarket`. The strategy ignores unfinished candles and keeps all state inside class fields to respect the StockSharp event-driven architecture.

## Risk Management
* **Stop-loss in pips** – Every entry installs a protective stop at `StopLossPips * PriceStep` away from the fill price.
* **Reward ratio take-profit** – The take-profit distance equals the stop distance multiplied by `RewardRatio` to maintain a fixed reward-to-risk relationship.
* **Trailing stop** – When enabled, the stop moves behind price by `TrailingStopPips` once the market advances at least that distance from the entry.
* **Break-even shift** – After `BreakEvenTriggerPips` of favorable travel, the stop is pushed to the entry plus an additional `BreakEvenOffsetPips` cushion (or minus for shorts), locking in gains.
* **Exit switch** – Setting `ExitSwitch` to `true` flattens the current position at the next completed bar and disables further processing until the flag is turned off.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume of each market order. |
| `CandleType` | `15m` time-frame | Primary candle series. |
| `FastMaPeriod` | `6` | Period of the fast LWMA. |
| `SlowMaPeriod` | `85` | Period of the slow LWMA. |
| `MomentumThreshold` | `0.3` | Minimum absolute distance of the momentum indicator from 100 needed to allow entries. |
| `RewardRatio` | `2` | Take-profit multiple relative to the stop-loss. |
| `StopLossPips` | `20` | Stop-loss distance in pips (PriceStep multiples). |
| `MaxPositions` | `10` | Maximum number of volume units (`TradeVolume`) allowed simultaneously. |
| `EnableTrailing` | `true` | Enables pip-based trailing stop updates. |
| `TrailingStopPips` | `40` | Trailing stop distance in pips. |
| `EnableBreakEven` | `true` | Activates break-even stop management. |
| `BreakEvenTriggerPips` | `30` | Profit (in pips) required before moving the stop to break-even. |
| `BreakEvenOffsetPips` | `30` | Extra pip offset added when the stop relocates to break-even. |
| `ExitSwitch` | `false` | Forces the strategy to flat all exposure on the next completed candle. |

## Workflow
1. Configure the desired instrument and candle series, then set risk parameters.
2. Start the strategy. It subscribes to candles, binds indicators, and begins processing completed bars.
3. When the entry conditions align, the engine submits a market order and stores stop/target levels.
4. On every finished candle the risk block evaluates trailing, break-even, and emergency exit rules.
5. Exits are triggered either by reaching stop/take-profit levels, trailing updates, break-even adjustments, or the emergency switch.

## Notes
* The conversion leverages StockSharp's indicator binding instead of manual buffer access, ensuring each indicator receives synchronized data.
* All calculations rely on the instrument's `PriceStep`. If the step is zero or missing, risk distances remain disabled to avoid invalid price levels.
* The strategy does not modify pending orders; it simply sends market orders to open/close positions, mirroring the way the original EA flattened exposure when thresholds were hit.
