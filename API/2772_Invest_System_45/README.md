# Invest System 4.5 Strategy (C#)

## Overview
Invest System 4.5 is a MetaTrader 5 expert advisor that has been ported to the StockSharp high-level strategy API. The strategy trades the EUR/USD pair by following the direction of the previous completed 4-hour candle. A single trade is allowed during the first minutes of the new 4-hour session and position sizing adapts to realized performance and account growth.

The code relies exclusively on the high-level API: automatic candle subscriptions are used to monitor both the 4-hour directional bias and the lower timeframe entry window, while the built-in `StartProtection` helper enforces static take-profit and stop-loss levels expressed in pips.

## Trading Logic
1. **Directional bias** – at the close of every finished 4-hour candle the strategy stores whether the candle closed bullish or bearish. A bullish candle enables only long entries for the next session, while a bearish candle enables only shorts. If the candle closes exactly at its open, the previous direction is kept.
2. **Entry timing** – when a new 4-hour candle starts, an entry window opens. The window remains valid for a configurable number of minutes (15 by default). The strategy watches lower timeframe candles (1 minute by default) and may submit at most one market order if all filters are satisfied while the window is active.
3. **Single position** – the strategy never pyramids. If a position is already open, no new signals are processed until the next 4-hour session. Once an order is sent the entry window closes immediately to replicate the MetaTrader behavior.
4. **Profit and loss tracking** – when a position is fully closed the realized PnL is captured to drive the adaptive lot logic described below.

## Position Sizing Rules
The original expert advisor uses two layers of money management:
- **Equity milestones**: the initial account balance is stored on the very first update. When the equity exceeds 2×, 3× … 6× the initial balance the base lot size is increased proportionally. Stage 1 starts at `BaseLot`, stage 2 doubles it, stage 3 triples it, and so on. Secondary lot sizes (`Lot2`, `Lot3`, `Lot4`) are derived using the original multipliers (×2, ×7 and ×14 respectively).
- **Plan B escalation**: a single global volume value is kept between trades.
  - After a losing trade with the base lot the volume is raised to the second lot (`Lot3`).
  - If another loss occurs while trading the second lot, “Plan B” activates. Plan B remaps the internal lot options so that the base lot becomes `Lot2` and the aggressive lot becomes `Lot4`. The current volume is not changed immediately, but any subsequent loss pushes the strategy to the aggressive lot. Plan B is cancelled automatically when the account hits a new equity high.
  - A profitable trade always resets the current volume back to the base lot for the active stage.
These rules closely reproduce the cascading lot escalation from the MetaTrader version without manually iterating through orders or using collections.

## Risk Management
- `StartProtection` configures both the stop-loss and the take-profit in absolute price units derived from the pip size. Stops and targets are registered only once when the strategy is started, just like the original EA attaches the values to each order.
- Only market orders are used. No hedge positions, scaling or partial exits are performed by the strategy itself; exits occur via the configured protective orders.

## Strategy Parameters
| Parameter | Description | Default | Optimization Range |
|-----------|-------------|---------|--------------------|
| `StopLossPips` | Stop-loss distance in pips. Use `0` to disable the stop. | 240 | 120 – 360, step 20 |
| `TakeProfitPips` | Take-profit distance in pips. Use `0` to disable the target. | 40 | 20 – 80, step 10 |
| `EntryWindowMinutes` | Length of the entry window after each new 4-hour candle opens. | 15 | 5 – 30, step 5 |
| `SignalCandleType` | Candle series used to monitor the entry window (1-minute by default). | 1-minute time frame | – |
| `TrendCandleType` | Higher timeframe candle used to build the directional bias (4-hour by default). | 4-hour time frame | – |
| `BaseLot` | Initial lot size for stage 1. Other lot sizes are derived automatically. | 0.1 | 0.05 – 0.3, step 0.05 |

## File Structure
```
2772_Invest_System_45/
├── CS/
│   └── InvestSystem45Strategy.cs
├── README.md
├── README_ru.md
└── README_zh.md
```

## Notes
- The strategy expects the attached security to provide both the 4-hour candle series and the faster timeframe series. These subscriptions are automatically created inside `OnStarted`.
- The pip size is determined from `Security.PriceStep` and adjusted for fractional quoting (3 or 5 decimal places) to match MetaTrader’s treatment of pip values.
- Because the original robot uses account balance thresholds, the StockSharp implementation reads `Portfolio.CurrentValue` on every entry candle update. When running in simulation make sure that the portfolio model updates the current equity so that the lot scaling remains consistent.
- Python translation is intentionally omitted as requested.
