# LotScalp Strategy

This strategy opens a single trade each day at a specified hour based on the difference between past candle opens.

## How It Works

1. **Waiting for Trade Time**: The strategy monitors candle open times. Once the hour is greater than `TradeTime`, trading is allowed for the next occurrence of that hour.
2. **Signal Generation**:
   - When the current hour equals `TradeTime`, the strategy compares the open price from `t1` bars ago with the open price from `t2` bars ago.
   - If the difference `Open[t1] - Open[t2]` exceeds `DeltaShort` points, a short position is opened.
   - If the difference `Open[t2] - Open[t1]` exceeds `DeltaLong` points, a long position is opened.
3. **Position Management**:
   - For long positions, the strategy exits when price reaches `TakeProfitLong` above the entry or `StopLossLong` below it.
   - For short positions, it exits when price moves `TakeProfitShort` below or `StopLossShort` above the entry.
   - Positions are also closed if they remain open longer than `MaxOpenTime` hours.

The strategy trades with a fixed volume and does not enter new trades until the next day.

## Parameters

| Name | Description |
| ---- | ----------- |
| `CandleType` | Candle source for the strategy. |
| `Volume` | Order volume. |
| `TakeProfitLong` | Take profit in points for long trades. |
| `StopLossLong` | Stop loss in points for long trades. |
| `TakeProfitShort` | Take profit in points for short trades. |
| `StopLossShort` | Stop loss in points for short trades. |
| `TradeTime` | Hour of the day when signals are evaluated. |
| `T1` | Number of bars back for the first open price. |
| `T2` | Number of bars back for the second open price. |
| `DeltaLong` | Minimum difference (in points) between `Open[t2]` and `Open[t1]` to open a long trade. |
| `DeltaShort` | Minimum difference (in points) between `Open[t1]` and `Open[t2]` to open a short trade. |
| `MaxOpenTime` | Maximum holding time in hours. |

## Notes

- Only finished candles are processed.
- The strategy uses the instrument price step to convert point-based thresholds into absolute prices.
- No additional indicators are used.
