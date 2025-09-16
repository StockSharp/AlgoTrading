# Altarius RSI Stochastic Strategy

## Overview
The Altarius RSI Stochastic Strategy is a direct conversion of the MetaTrader 5 expert advisor "Altarius RSI Stohastic" into StockSharp's high-level API. The system synchronizes two Stochastic oscillators with a fast 3-period RSI to capture short-lived reversals that occur when momentum compresses and then expands again. The StockSharp implementation preserves the original entry and exit logic while adding modern conveniences such as strategy parameters, automatic risk management, and adaptive position sizing.

## How It Works
- **Primary Stochastic (15/8/8):** Acts as the trend filter. Long positions require the %K line to be below 50 yet crossing above the %D line, signalling upward momentum inside a neutral-to-oversold zone. Short positions require the mirror condition above 55.
- **Secondary Stochastic (10/3/3):** Measures how strongly %K diverges from %D. A minimum absolute gap of 5 points is required to validate momentum before entering a position.
- **RSI (Period 3):** Controls exits. Long positions close when RSI exceeds 60 and the primary %D turns down from above 70. Short positions exit when RSI falls below 40 and the primary %D turns up from below 30.
- **Drawdown Guard:** If floating PnL drops below the configurable risk multiple of the account equity, the strategy immediately liquidates the open position—similar to the emergency stop in the original code.
- **Adaptive Sizing:** Initial volume is derived from portfolio equity multiplied by the `MaximumRisk` factor and divided by 1000, matching the MT5 approach. Consecutive losing trades shrink the position size according to the `DecreaseFactor`, while respecting a minimum tradable volume.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Time frame used for candle subscriptions. | 5-minute time frame |
| `BaseVolume` | Fallback volume used when portfolio information is unavailable. | 0.1 |
| `MinimumVolume` | Minimum volume allowed after all calculations. | 0.1 |
| `MaximumRisk` | Risk multiplier applied to portfolio value for sizing and drawdown exit. | 0.1 |
| `DecreaseFactor` | Divider that reduces volume after consecutive losing trades. | 3 |
| `PrimaryStochasticLength` | Lookback period for the primary Stochastic %K line. | 15 |
| `PrimaryStochasticKPeriod` | Smoothing for the primary %K line. | 8 |
| `PrimaryStochasticDPeriod` | Period for the primary %D signal line. | 8 |
| `SecondaryStochasticLength` | Lookback period for the confirmation Stochastic. | 10 |
| `SecondaryStochasticKPeriod` | Smoothing for the secondary %K line. | 3 |
| `SecondaryStochasticDPeriod` | Period for the secondary %D line. | 3 |
| `DifferenceThreshold` | Minimum gap between secondary %K and %D to allow entries. | 5 |
| `PrimaryBuyLimit` | Maximum primary %K value allowed before opening a long. | 50 |
| `PrimarySellLimit` | Minimum primary %K value allowed before opening a short. | 55 |
| `PrimaryExitUpper` | Primary %D threshold that must be exceeded before closing longs. | 70 |
| `PrimaryExitLower` | Primary %D threshold that must be undershot before closing shorts. | 30 |
| `RsiPeriod` | RSI lookback length. | 3 |
| `LongExitRsi` | RSI level that confirms long exits. | 60 |
| `ShortExitRsi` | RSI level that confirms short exits. | 40 |

## Trading Rules
1. **Entry Conditions**
   - **Long:** Primary %K > primary %D, primary %K < `PrimaryBuyLimit`, and |secondary %K − secondary %D| > `DifferenceThreshold` while the strategy is flat.
   - **Short:** Primary %K < primary %D, primary %K > `PrimarySellLimit`, and |secondary %K − secondary %D| > `DifferenceThreshold` while the strategy is flat.
2. **Exit Conditions**
   - **Long Exit:** RSI > `LongExitRsi`, primary %D > `PrimaryExitUpper`, and the current %D is lower than the previous candle's value.
   - **Short Exit:** RSI < `ShortExitRsi`, primary %D < `PrimaryExitLower`, and the current %D is higher than the previous candle's value.
   - **Risk Exit:** When the floating loss exceeds `MaximumRisk × Portfolio.CurrentValue`.

## Risk Management
- The strategy automatically calls `StartProtection()` to engage StockSharp's built-in position protection services.
- Position size shrinks when `_lossStreak` exceeds one losing trade in a row, mimicking the MT5 `DecreaseFactor` logic.
- `MinimumVolume` prevents the position size from collapsing below exchange tick size requirements.

## Notes
- The strategy assumes a hedging-capable portfolio, exactly like the original EA.
- Customize the `CandleType` parameter to match the timeframe you would have used in MetaTrader (M1, M5, etc.).
- Combine this module with StockSharp Designer or the Backtester project in this repository to validate the performance on your own data.
