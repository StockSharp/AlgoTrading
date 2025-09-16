# KDJ Expert Advisor Strategy

## Overview
This strategy replicates the MetaTrader 5 "KDJ Expert Advisor" by senlin ge. It trades a single symbol using signals from the KDJ oscillator, an evolution of the stochastic oscillator where the %K line is smoothed twice. The strategy observes the difference between the %K and %D lines (often called the J line) to identify momentum reversals, opening only one position at a time. Trade management mirrors the original expert advisor: each trade immediately receives a fixed stop-loss and take-profit that are expressed in pips and translated into price distance using the instrument settings.

The implementation uses StockSharp's high-level API with a candle subscription and the built-in `Stochastic` indicator, configured to match the KDJ parameters from the MQL5 version. The code automatically detects 3- or 5-digit Forex symbols and adjusts the pip value accordingly.

## Indicator Logic
The underlying indicator works in three stages:

1. **RSV calculation** – For each finished candle, compute the Raw Stochastic Value over `KDJ Length` candles:
   \[
   RSV = \frac{Close - LowestLow}{HighestHigh - LowestLow} \times 100
   \]
2. **%K smoothing** – Average the last `Smooth %K` RSV values to obtain the %K line.
3. **%D smoothing** – Average the last `Smooth %D` %K values to obtain the %D line.

The strategy then analyses `K - D` (referred to as *KDC* in the original source) and the slope of %K to detect reversals.

## Entry Rules
A market position is opened only if there is no existing position for the symbol. Signals are evaluated on completed candles:

- **Buy** when either of the following conditions is true:
  - `K - D` crosses above zero (from negative to positive); or
  - `K - D` is above zero and the %K line is rising (`K_current > K_previous`).
- **Sell** when either of the following conditions is true:
  - `K - D` crosses below zero (from positive to negative); or
  - `K - D` is below zero and the %K line is falling (`K_current < K_previous`).

This matches the boolean structure from the original MQL5 expert advisor, ensuring identical trade timing.

## Risk Management
- Each filled order receives a protective stop-loss and take-profit, measured in pips and converted into price distance via the instrument's tick size. A value of zero disables the corresponding protection leg.
- The strategy does not pyramid or average positions. It remains flat until the current position is closed by the protective orders or by manual intervention.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| **Candle Type** | Data type/timeframe of the input candles. | 15-minute time frame |
| **KDJ Length** | Number of candles for RSV calculation. | 30 |
| **Smooth %K** | Number of RSV values used to smooth the %K line. | 3 |
| **Smooth %D** | Number of %K values used to smooth the %D line. | 6 |
| **Stop Loss (pips)** | Distance of the protective stop-loss. Set to 0 to disable. | 25 |
| **Take Profit (pips)** | Distance of the protective take-profit. Set to 0 to disable. | 45 |
| **Order Volume** | Quantity sent with market orders. | 1 |

All parameters support optimization ranges identical to the original expert's inputs.

## Usage Notes
1. Configure the desired security and connector in the tester or live environment.
2. Adjust the candle type to match the chart timeframe you want to emulate from MetaTrader.
3. Optionally optimize the KDJ parameters, stop-loss, take-profit, or order volume.
4. Start the strategy. Orders are generated only on fully formed candles.
5. The chart automatically displays candles, the KDJ indicator, and executed trades for visual confirmation.

## Differences from the Original EA
- Uses StockSharp's `Stochastic` indicator with smoothing periods to replicate the MQL5 KDJ buffers; no external indicator file is required.
- Protective orders are managed through `StartProtection`, which submits market exits when triggered.
- Volume is a fixed parameter instead of the MQL5 `MoneyFixedMargin` risk model, keeping the implementation concise and focused on the signal logic.
