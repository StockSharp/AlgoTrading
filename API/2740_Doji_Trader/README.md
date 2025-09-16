# Doji Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the core logic of the classic **Doji Trader** expert advisor.
It monitors completed candles for compact-bodied doji patterns and waits for a breakout
close beyond the doji range to enter the market in the breakout direction.

## Trading Logic

1. Only finished candles are processed. The default timeframe is 1 hour, but it can be
   adjusted through the `CandleType` parameter.
2. Trading is allowed only when the closing time of the latest candle falls within the
   configurable session window `[StartHour, EndHour)` measured in exchange time.
3. The algorithm keeps the three most recent finished candles in memory. The candle that
   just closed is compared against the two candles that came before it (`-2` and `-3`).
4. A candle counts as a doji when the absolute difference between its open and close is
   lower than `MaximumDojiHeight * pip`, where the pip value is derived from the
   security price step (3- or 5-digit quotes are automatically scaled by ×10).
5. If the newest candle closes **above** the high of the most recent qualifying doji, the
   strategy opens (or flips into) a long position. If it closes **below** the doji low, it
   opens a short position. No trade is placed when price remains inside the doji range.
6. The position size is taken from the strategy `Volume` property. When a reversal signal
   appears, the algorithm sends enough volume to close the previous position and establish
   the desired exposure in the new direction so only one net position stays open.

## Risk Management

- Stop-loss and take-profit distances are configured in pips through `StopLossPips` and
  `TakeProfitPips`. Setting a value to zero disables the corresponding protective order.
- `StartProtection` is launched once at startup and uses market orders for exits so the
  behaviour mirrors the MQL implementation that closed and reopened positions directly.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe of processed candles. | 1 hour time frame |
| `StartHour` | Inclusive opening hour of the trading window. | 8 |
| `EndHour` | Exclusive closing hour of the trading window. | 17 |
| `MaximumDojiHeight` | Maximum body height (in pips) for a candle to be treated as a doji. | 1 |
| `StopLossPips` | Protective stop distance in pips. | 50 |
| `TakeProfitPips` | Profit target distance in pips. | 50 |

### Additional Notes

- The strategy assumes the platform account uses netted positions. If your feed provides
  fractional pip steps (5-digit or 3-digit quotes), the pip value is multiplied by 10 to
  match traditional pip measurements.
- Set the desired lot size in the `Volume` property before running the strategy.
- No additional indicators are required; the logic depends only on raw candle data.
- There is no Python port yet per request, only the C# implementation.
