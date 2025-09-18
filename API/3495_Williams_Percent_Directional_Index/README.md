# Williams Percent Directional Index Strategy

## Overview
The **Williams Percent Directional Index Strategy** recreates the MetaTrader 5 expert "Mt5 Williams % Directional Index EA" using StockSharp's high-level API. It combines the Williams %R oscillator with the Average Directional Index (ADX) to identify momentum turns and then relies on the Money Flow Index (MFI) and Stochastic Oscillator to exit trades. The implementation processes only finished candles and uses indicator bindings so every decision is based on the latest completed bar.

## Trading Logic
1. **Trend Alignment**
   - Williams %R must be rising for long trades or falling for short trades. The strategy compares the values from the two previously finished bars to assess the momentum slope.
   - The directional movement component of the ADX (`+DI - -DI`) must have crossed zero on the last closed bar: a negative to positive transition confirms bullish momentum, while a positive to negative transition confirms bearish momentum.
2. **Entry Rules**
   - If both bullish conditions are satisfied and the current position is flat or short, the strategy opens a market buy order.
   - If both bearish conditions are satisfied and the current position is flat or long, the strategy opens a market sell order.
   - When both long and short signals appear simultaneously (rare, but possible on identical values), the trade is skipped to avoid conflicting instructions.
3. **Exit Rules**
   - Long positions close when either the MFI value from two bars ago exceeds the overbought level or the Stochastic main line forms a local trough pattern (`K[−2] > K[−1] < K[0]`).
   - Short positions close when either the MFI value from two bars ago drops below the mirrored oversold level (`100 - level`) or the Stochastic main line forms a local peak pattern (`K[−2] < K[−1] > K[0]`).
4. **Risk Handling**
   - The conversion keeps the entry and exit mechanics of the original expert advisor. Stop-loss and trailing features from the MQL source are not reproduced; risk control should be managed externally or added via StockSharp protections if required.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Candle Type` | Time frame for all indicator calculations. | 15-minute time frame |
| `Williams %R Period` | Look-back period used in the Williams %R oscillator. | 42 |
| `Directional Period` | Period for ADX calculations (affects +DI/−DI). | 20 |
| `MFI Period` | Length of the Money Flow Index. | 19 |
| `MFI Level` | Overbought threshold used to trigger exits. The oversold level is computed as `100 - value`. | 79 |
| `Stochastic %K` | %K period of the stochastic oscillator. | 22 |
| `Stochastic %D` | %D period of the stochastic oscillator. | 16 |
| `Stochastic Smoothing` | Additional smoothing ("slowing") applied to the stochastic oscillator. | 21 |

All parameters are exposed as `StrategyParam` values, so they can be optimized or adjusted through the StockSharp GUI.

## Usage Notes
- Bind the strategy to any instrument and set an appropriate volume before starting.
- The strategy processes only completed candles (`CandleStates.Finished`), guaranteeing indicator values are final.
- Chart rendering is enabled: Williams %R, ADX, MFI, Stochastic, and executed trades are plotted when a chart area is available.
- To recreate the original MT5 behaviour regarding stop management, consider adding `StartProtection` or custom risk logic as needed.

## Differences from the MQL Version
- The StockSharp conversion uses indicator bindings instead of manual buffer copying, but the logical checks, including zero-cross validation and multi-bar patterns, follow the MT5 expert advisor.
- Session filters, retry logic, and trailing stop management from the MQL code are intentionally omitted to focus on the core signal engine requested for this conversion.
