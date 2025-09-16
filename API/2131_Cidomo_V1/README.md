# Cidomo V1

Daily breakout strategy that places trades when price escapes the recent range.

## Summary

- **Type**: Breakout
- **Entry**: Buy when price breaks above the highest high of the lookback period, sell when price breaks below the lowest low.
- **Exit**: Stop loss, take profit, optional breakeven and trailing stop.
- **Indicators**: Highest, Lowest

## Parameters

| Name | Description |
|------|-------------|
| `Lookback` | Number of candles used to calculate the range. |
| `Delta` | Price offset added to breakout levels. |
| `StopLoss` | Stop loss in price points. |
| `TakeProfit` | Take profit in price points. |
| `NoLoss` | Move stop to entry after this profit (points). |
| `Trailing` | Trailing distance in points. |
| `UseTimeFilter` | If true, levels are calculated after the specified time. |
| `TradeTime` | Time of day to calculate breakout levels. |
| `CandleType` | Candle type used for calculations. |

## Notes

The strategy monitors finished candles only. Levels are recalculated once per day after `TradeTime`.
