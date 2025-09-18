# GBPCHF Dual-MACD Correlation Strategy

## Overview
This strategy trades the GBP/CHF cross (the strategy security) by analysing the relationship between two correlated currency pairs:

- **GBPUSD** provides the leading momentum signal.
- **USDCHF** acts as the confirming filter.

Both instruments are processed through a classic MACD (12, 26, 9) indicator on the same candle series. A long position is opened when the GBPUSD MACD line crosses above the USDCHF MACD line while both are positive, suggesting broad-based sterling strength against the dollar and Swiss franc. A short position is opened when the GBPUSD MACD line crosses below the USDCHF MACD line while both are negative, signalling sterling weakness. Optional reversal mode inverts these conditions.

## Trading Logic
1. Subscribes to the configured candle type (hourly by default) for three securities: the traded GBPCHF symbol, GBPUSD, and USDCHF.
2. Binds MACD indicators to GBPUSD and USDCHF candle streams.
3. When both indicators are formed:
   - **Bullish entry**: previous MACD values above zero and the GBPUSD line crosses above the USDCHF line.
   - **Bearish entry**: previous MACD values below zero and the GBPUSD line crosses below the USDCHF line.
4. Entries respect the `OnlyOnePosition` and `CloseOppositePositions` flags to control exposure.
5. Exit management uses configurable stop-loss and take-profit levels, plus an optional trailing stop that updates either every N seconds or once per bar.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `StopLossPips` | Initial protective stop in pips. | 70 |
| `TakeProfitPips` | Profit target distance in pips. | 45 |
| `TrailingFrequencySeconds` | Minimum seconds between trailing updates (values &lt; 10 restrict updates to one per bar). | 10 |
| `TrailingStopPips` | Distance for the trailing stop. Set to 0 to disable trailing. | 0 |
| `TrailingStepPips` | Minimum favourable move (in pips) before the trailing stop is advanced. | 5 |
| `ReverseSignals` | Invert long/short conditions. | false |
| `CloseOppositePositions` | Close opposite exposure before entering a new trade. | false |
| `OnlyOnePosition` | Restrict the strategy to a single simultaneous position. | true |
| `TradeVolume` | Order volume for market entries. | 1 |
| `MacdShortPeriod` / `MacdLongPeriod` / `MacdSignalPeriod` | MACD configuration shared by both correlation legs. | 12 / 26 / 9 |
| `CandleType` | Candle type used for all indicators. | 1-hour time frame |
| `GbpUsdSecurity` | Security representing GBPUSD prices. | *Required* |
| `UsdChfSecurity` | Security representing USDCHF prices. | *Required* |

## Position Management
- Stop-loss defaults to `StopLossPips`, or falls back to `TrailingStopPips` if the fixed stop is disabled.
- Take-profit is placed at `TakeProfitPips` when greater than zero.
- Trailing stop advances in increments of `TrailingStepPips` once the price moves by `TrailingStopPips` in favour of the position and the trailing frequency criteria are met.
- When an exit condition triggers (stop, trail, or target), the position is closed using market orders.

## Usage Notes
- Ensure that the GBPUSD and USDCHF securities deliver the same candle type as the traded GBPCHF instrument to keep the correlation calculation aligned.
- Enable `CloseOppositePositions` when running in environments that disallow hedging or when you prefer clean single-direction exposure.
- The trailing mechanism runs on finished candles; for intrabar trailing use a smaller candle type or adjust the frequency parameter accordingly.
