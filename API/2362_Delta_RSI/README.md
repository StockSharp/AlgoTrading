# Delta RSI Strategy

This strategy trades based on the **Delta RSI** indicator. Two RSI indicators with different periods are compared:

- **Fast RSI** reacts quickly to price changes.
- **Slow RSI** acts as a trend filter.

A long position is opened on the bar following an **Up** signal when:

1. Slow RSI is above the `Level` threshold.
2. Fast RSI is higher than the slow RSI.
3. Previous bar showed the Up state and current bar is no longer Up.

A short position is opened on the bar following a **Down** signal when:

1. Slow RSI is below `100 - Level`.
2. Fast RSI is below the slow RSI.
3. Previous bar showed the Down state and current bar is no longer Down.

Optional flags allow enabling or disabling opening and closing of long and short positions separately.

## Parameters

| Name | Description |
|------|-------------|
| `FastPeriod` | Fast RSI period. |
| `SlowPeriod` | Slow RSI period. |
| `Level` | Threshold level for the slow RSI. |
| `BuyPosOpen` / `SellPosOpen` | Allow opening long/short positions. |
| `BuyPosClose` / `SellPosClose` | Allow closing long/short positions. |
| `CandleType` | Timeframe of input candles. |

The strategy subscribes to candles of the selected timeframe, calculates both RSI values, and processes signals on each finished candle. When a signal appears, the strategy optionally closes the opposite position and opens a new one in the signal direction.
