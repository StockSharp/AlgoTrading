# Universal Investor Strategy

This strategy is a direct port of the **Universal Investor** MetaTrader 4 expert advisor. It combines an exponential moving average (EMA) and a linear weighted moving average (LWMA) to confirm short-term trend direction and performs one-position trading with adaptive position sizing.

## Trading logic

1. Subscribe to the configured `CandleType` and compute both EMA and LWMA with the period defined by `MovingPeriod`.
2. Store the two most recent values of each moving average so that the logic mimics the `iMA(..., shift = 1/2)` calls from the original EA.
3. Generate a **buy** signal when the previous LWMA is above the previous EMA, both averages were rising, and there is no opposite signal on the same candle.
4. Generate a **sell** signal when the previous LWMA is below the previous EMA, both averages were falling, and there is no opposite signal on the same candle.
5. Close an open long position as soon as the LWMA drops below the EMA (mirror logic for shorts).
6. Calculate the trade volume from the strategy `Volume` parameter, increase it to satisfy the `MaximumRisk` requirement when the portfolio value is large enough, and reduce it after consecutive losing trades according to `DecreaseFactor`.
7. Submit market orders with `BuyMarket`/`SellMarket` and keep track of the entry price to detect winning or losing exits.

The strategy keeps only one position open at a time and immediately reverses only after a full close, reproducing the behaviour of the original MetaTrader script.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Candle series used for calculations. |
| `MovingPeriod` | Period for both EMA and LWMA. |
| `MaximumRisk` | Fraction of equity (0.05 = 5%) used to compute the minimum position volume. |
| `DecreaseFactor` | Reduces the volume after consecutive losing trades (0 disables the feature). |
| `Volume` | Base contract volume passed to `BuyMarket`/`SellMarket`. |

## Indicators

- `ExponentialMovingAverage`
- `LinearWeightedMovingAverage`

## Notes

- Orders are placed only on closed candles, matching the EA that relies on `Time[0]` checks.
- The position size logic mirrors the MetaTrader `LotsOptimized` function, including the risk-based component and the loss streak multiplier.
