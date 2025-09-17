# BB Swing Strategy

## Overview

The **BB Swing Strategy** is a faithful port of the MetaTrader "BB SWING" expert advisor. It trades Bollinger band pullbacks that align with the prevailing trend defined by two linear weighted moving averages (LWMAs). A higher-timeframe momentum filter and a very slow MACD help confirm the strength of the reversal before any position is opened.

## Trading Logic

1. Work only with finished candles from the `CandleType` timeframe.
2. Track the last four completed candles to inspect recent extremes and candle bodies.
3. Wait for the fast LWMA to stay above (for longs) or below (for shorts) the slow LWMA.
4. Check that one of the last three lows touches the lower Bollinger band (long setup) or one of the highs touches the upper band (short setup).
5. Require that the previous candle has a stronger body than its predecessor, signalling momentum away from the band.
6. Confirm the trend strength with momentum calculated on `MomentumCandleType`. The strategy measures the absolute distance between the momentum reading and 100; the distance must exceed the configured buy/sell thresholds on any of the latest three momentum values.
7. Validate the long-term direction with a MACD calculated on the `MacdCandleType` timeframe. Long entries are allowed while the MACD main line stays above the signal line; shorts require the opposite relation.
8. When all conditions align, enter a market position using the current martingale volume step.

## Position Sizing and Scaling

- `InitialVolume` defines the first entry volume.
- Every additional add-on multiplies the base volume by `LotExponent` (`volume = InitialVolume * LotExponent^n`).
- `MaxTrades` caps the number of sequential add-ons so the total position size never exceeds `InitialVolume * MaxTrades`.

## Exit and Protection Rules

- Fixed `StopLoss` and `TakeProfit` values expressed in price steps.
- Optional break-even logic (`EnableBreakEven`) that moves the stop to `BreakEvenOffset` once price advances by `BreakEvenTrigger` steps.
- Classic trailing stop (`EnableTrailingStop`) that follows the extreme price by `TrailingStop` steps.
- Money management tools:
  - `UseMoneyTakeProfit` closes positions once unrealised profit in account currency reaches `MoneyTakeProfit`.
  - `UsePercentTakeProfit` closes positions once profit equals `PercentTakeProfit` percent of the starting equity.
  - `UseMoneyTrailing` activates a profit trail: once profit exceeds `MoneyTrailTarget`, a pullback of `MoneyTrailStop` triggers an exit.
- `UseEquityStop` monitors equity drawdown relative to the equity peak recorded during the session. A drawdown larger than `EquityRiskPercent` closes all positions.
- Optional `CloseOnMacdCross` exits whenever the MACD main line crosses the signal line against the current position direction.

All protective actions rely on market orders (`BuyMarket` / `SellMarket`) to neutralise the entire position.

## Parameters

| Name | Description |
|------|-------------|
| `InitialVolume` | Base trade volume used for the first entry. |
| `LotExponent` | Multiplier applied to the volume of each additional entry while scaling in. |
| `MaxTrades` | Maximum number of sequential add-ons allowed at any time. |
| `TakeProfit` | Take profit expressed in price steps. |
| `StopLoss` | Stop loss expressed in price steps. |
| `FastMaPeriod` | Period of the fast LWMA calculated on typical prices. |
| `SlowMaPeriod` | Period of the slow LWMA calculated on typical prices. |
| `MomentumLength` | Number of bars used in the momentum calculation. |
| `MomentumBuyThreshold` | Minimum distance from 100 for the higher-timeframe momentum to validate long trades. |
| `MomentumSellThreshold` | Minimum distance from 100 for the higher-timeframe momentum to validate short trades. |
| `EnableBreakEven` | Enables break-even stop movement. |
| `BreakEvenTrigger` | Price steps required to trigger the break-even move. |
| `BreakEvenOffset` | Offset applied to the stop once break-even activates. |
| `EnableTrailingStop` | Enables the classic trailing stop in price steps. |
| `TrailingStop` | Size of the trailing stop expressed in steps. |
| `UseMoneyTakeProfit` | Enables fixed profit taking in account currency. |
| `MoneyTakeProfit` | Profit in currency that closes the position when `UseMoneyTakeProfit` is active. |
| `UsePercentTakeProfit` | Enables equity-percentage-based profit taking. |
| `PercentTakeProfit` | Percentage of the initial equity that triggers an exit when `UsePercentTakeProfit` is active. |
| `UseMoneyTrailing` | Enables money-based trailing after a target profit is reached. |
| `MoneyTrailTarget` | Profit level that activates the money trailing logic. |
| `MoneyTrailStop` | Maximum allowed pullback in currency after activation. |
| `UseEquityStop` | Enables closing positions when the floating drawdown exceeds a threshold. |
| `EquityRiskPercent` | Maximum permitted equity drawdown in percent. |
| `CloseOnMacdCross` | Enables MACD-based exit filtering. |
| `CandleType` | Primary timeframe used for signal calculations. |
| `MomentumCandleType` | Higher timeframe used for the momentum filter. |
| `MacdCandleType` | Very slow timeframe used by the MACD exit filter. |

## Notes

- The strategy processes finished candles only; it does not react intrabar.
- All stop and target calculations use the instrument price step reported by the connected exchange. Ensure `PriceStep` is configured correctly for precise risk control.
- Money- and equity-based protections rely on the strategy portfolio statistics available in StockSharp. When running in tester mode make sure the portfolio feed is enabled.
- Unlike the original MQL expert, this C# implementation maintains a single aggregated position per direction. Scaling in increases the aggregate position instead of opening multiple discrete tickets.
- Bollinger bands use a fixed length of 20 and width of 2 standard deviations on typical prices, matching the original code.
