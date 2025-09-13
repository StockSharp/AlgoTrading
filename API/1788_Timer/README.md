# Timer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Timer strategy recalculates breakout levels at fixed time intervals and trades when price crosses these dynamic thresholds. The levels are positioned using Average True Range (ATR) and an optional additional pip distance. The approach seeks to capture short-term breakouts in either direction.

Every `WaitSeconds`, the strategy sets:
- **Buy level** at `close + pipDistance + ATR`.
- **Sell level** at `close - pipDistance - ATR`.

When the next finished candle closes beyond one of these levels, a market order is placed in the corresponding direction. The position is protected by configurable stop-loss, take-profit, and trailing stop distances.

Trading can be limited to a specific time window using the trading hours settings.

## Parameters
- `WaitSeconds` – seconds between level recalculations.
- `PipDistance` – additional distance from the current price, in points.
- `AtrPeriod` – ATR indicator period.
- `TakeProfit` – take-profit distance in points.
- `StopLoss` – stop-loss distance in points.
- `TrailingStop` – trailing stop distance in points.
- `TradeVolume` – order volume.
- `CandleType` – candle type for calculations.
- `UseTradingHours` – enable time-of-day filter.
- `StartTime` – trading start time.
- `StopTime` – trading stop time.

## How It Works
1. Subscribes to candles and calculates ATR.
2. On each finished candle:
   - If the configured time interval passed, new buy and sell levels are calculated.
   - If trading hours are enabled, checks that current time is within the allowed window.
   - Places buy or sell market order if price crosses the corresponding level.
3. Stop-loss, take-profit, and trailing stop are managed automatically by the strategy infrastructure.

## Notes
- The strategy trades both long and short.
- Works on any instrument and timeframe.
- ATR-based levels adapt to market volatility, allowing flexible breakout detection.
