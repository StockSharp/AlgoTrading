# AIS1 EURUSD Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the original AIS1 "A System: EURUSD Daily Metrics" expert advisor using StockSharp's high-level API. It trades EURUSD breakouts by comparing the current price action to the previous day's range and manages trades with adaptive position sizing plus a four-hour trailing stop.

## Strategy Overview

- **Market**: EURUSD spot/CFD/forex instruments.
- **Primary timeframe**: Daily candles provide the reference high, low, and close.
- **Secondary timeframe**: 4-hour candles drive trailing-stop updates and entry checks.
- **Direction**: Long and short trades are allowed.
- **Style**: Breakout continuation with volatility-scaled targets and stops.

## Trading Logic

1. Track the previous completed daily candle. Calculate the midpoint, range, and derived stop/take distances using configurable multipliers (`StopFactor`, `TakeFactor`).
2. Evaluate every completed 4-hour candle:
   - **Long entry**: Previous daily close is above the midpoint and the 4-hour high breaks above the previous daily high.
   - **Short entry**: Previous daily close is below the midpoint and the 4-hour low breaks below the previous daily low.
3. Position size is determined from the current portfolio equity and the configured risk share (`OrderReserve`). The volume is rounded to instrument trading steps.
4. For open positions the strategy applies three layers of exit control:
   - Fixed stop-loss at the opposite side of the daily range scaled by `StopFactor`.
   - Fixed take-profit at a distance of `TakeFactor` × daily range.
   - Dynamic trailing stop using the previous 4-hour range multiplied by `TrailFactor`. The trailing stop activates only after the trade moves in profit.
5. A five-second cooldown after any trade or exit mirrors the original EA behaviour and prevents rapid-fire modifications.

## Risk Management

- `OrderReserve` defines the fraction of current equity that can be risked on the next trade. If the calculated size is below the instrument minimum, the trade is skipped.
- `AccountReserve` tracks the peak equity and stops opening or managing trades once the equity drawdown exceeds `AccountReserve - OrderReserve` (16% with default inputs).
- Trailing exits and fixed targets ensure positions are closed even if new trades are blocked by the drawdown guard.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `AccountReserve` | Portion of equity excluded from trading, used to compute the allowed drawdown before trading pauses. |
| `OrderReserve` | Share of equity risked per trade. Determines the maximum loss using the stop distance. |
| `TakeFactor` | Multiplier applied to the previous daily range to set the take-profit distance. |
| `StopFactor` | Multiplier applied to the previous daily range to set the stop-loss distance. |
| `TrailFactor` | Multiplier applied to the previous 4-hour range to move the trailing stop once the position is profitable. |
| `EntryCandleType` | Candle type (default daily) used for breakout levels. |
| `TrailCandleType` | Candle type (default 4-hour) used for intraday evaluation and trailing. |

## Notes on the Conversion

- The StockSharp version triggers entries and trailing updates on completed 4-hour candles. The original MQL expert advisor reacted to every tick; using candles keeps the logic robust within the high-level API.
- Stop-loss, take-profit, and trailing exits are executed with market orders when the respective price levels are touched inside the processed candle.
- Margin checks from the MQL version are replaced with equity-based sizing to remain platform-neutral while respecting the original risk constraints.
