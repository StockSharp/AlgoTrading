# Exp ATR Trailing Strategy

This example demonstrates how to manage existing positions with a trailing stop based on the **Average True Range (ATR)** indicator. The strategy does not generate entry signals; it only adjusts the exit level of an open position according to market volatility.

## How it works

1. The strategy subscribes to candle data of a chosen timeframe.
2. An `AverageTrueRange` indicator is calculated on each candle.
3. For long positions the stop level is moved up to `Close - ATR * BuyFactor`.
4. For short positions the stop level is moved down to `Close + ATR * SellFactor`.
5. When price crosses the trailing level the position is closed at market.

The trailing stop only moves in the direction of the trade and never retreats, providing a volatility adjusted exit.

## Parameters

| Name | Description |
| --- | --- |
| `AtrPeriod` | ATR calculation period. |
| `BuyFactor` | Multiplier applied to ATR when trailing a long position. |
| `SellFactor` | Multiplier applied to ATR when trailing a short position. |
| `CandleType` | Timeframe of candles used for analysis. |

## Usage notes

- Attach the strategy to a security and open a position manually or from another strategy.
- Suitable for risk management where exits are controlled separately from entries.
- Chart area displays candles, ATR values and executed trades for visual analysis.

## References

- [Average True Range on StockSharp documentation](https://doc.stocksharp.com/topics/indicator_average_true_range.html)
- [Strategy Designer](https://doc.stocksharp.com/topics/designer.html)
