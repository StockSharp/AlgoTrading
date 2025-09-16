# EA Moving Average Strategy

## Overview
- Converted from the MetaTrader expert advisor **"EA Moving Average"** (barabashkakvn edition).
- Uses four independent moving averages to control long and short entries and exits.
- Designed for a single symbol in netting mode. The default candle type is 15‑minute time frame, but any regular candle type can be selected.
- The strategy opens at most one position at a time. While a position is active, only the exit rules are evaluated.

## Trading Logic
### Long Entry
1. The current candle must close above the *Buy Open* moving average after opening below it (true crossover inside a single bar).
2. `UseBuy` must be enabled.
3. If `ConsiderPriceLastOut` is enabled, the current price must be less than or equal to the price of the last closed trade. This prevents buying back above the most recent exit.
4. When conditions are satisfied the strategy submits a market buy order sized by the risk model.

### Long Exit
1. Active only while the net position is long.
2. The candle must open above the *Buy Close* moving average and close back below it, signalling a bearish crossover.
3. When triggered the entire position is closed with a market order.

### Short Entry
1. The candle must close below the *Sell Open* moving average after opening above it.
2. `UseSell` must be enabled.
3. If `ConsiderPriceLastOut` is enabled, the current price must be greater than or equal to the last exit price. This avoids shorting lower than the previous cover.
4. An at-market sell order is submitted using the risk-based volume.

### Short Exit
1. Active only while the position is short.
2. The candle must open below the *Sell Close* moving average and close above it.
3. The short position is fully covered at market.

## Risk and Position Sizing
- `MaximumRisk` expresses the risk capital per trade as a fraction of portfolio equity. The strategy divides this risk amount by the current price to obtain a raw volume estimate.
- `DecreaseFactor` emulates the original MetaTrader lot reduction. After two or more consecutive losing trades the volume is reduced proportionally to the loss streak divided by `DecreaseFactor`.
- Volumes are aligned to the instrument volume step and never drop below one step. If the risk calculation fails, the fallback is the strategy `Volume` property (default 1 contract/lot).

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaximumRisk` | `0.02` | Fraction of equity risked per trade. |
| `DecreaseFactor` | `3` | Lot reduction factor after consecutive losses. Use `0` to disable. |
| `BuyOpenPeriod` | `30` | Period of the moving average used for long entries. |
| `BuyOpenShift` | `3` | Forward shift (bars) applied to the long entry moving average. |
| `BuyOpenMethod` | `Exponential` | Moving average method for long entries (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `BuyOpenPrice` | `Close` | Price input for the long entry moving average. |
| `BuyClosePeriod` | `14` | Period of the long exit moving average. |
| `BuyCloseShift` | `3` | Shift (bars) applied to the long exit moving average. |
| `BuyCloseMethod` | `Exponential` | Method of the long exit moving average. |
| `BuyClosePrice` | `Close` | Price input for the long exit moving average. |
| `SellOpenPeriod` | `30` | Period of the short entry moving average. |
| `SellOpenShift` | `0` | Shift (bars) applied to the short entry moving average. |
| `SellOpenMethod` | `Exponential` | Method of the short entry moving average. |
| `SellOpenPrice` | `Close` | Price input for the short entry moving average. |
| `SellClosePeriod` | `20` | Period of the short exit moving average. |
| `SellCloseShift` | `2` | Shift (bars) applied to the short exit moving average. |
| `SellCloseMethod` | `Exponential` | Method of the short exit moving average. |
| `SellClosePrice` | `Close` | Price input for the short exit moving average. |
| `UseBuy` | `true` | Enable or disable long trades. |
| `UseSell` | `true` | Enable or disable short trades. |
| `ConsiderPriceLastOut` | `true` | Require price improvement relative to the last exit before re-entering. |
| `CandleType` | `15m` time frame | Candle series used for calculations. |

## Additional Notes
- The last exit price and consecutive loss counter are tracked from trade executions, mirroring the MetaTrader behaviour.
- Because StockSharp executes on finished candles, the entry price filter compares against the candle close price, which approximates the original tick-based ask/bid comparison.
- The strategy assumes a netting account; hedging multiple positions simultaneously is not supported.
- Always validate the configuration with historical tests before trading live capital.
