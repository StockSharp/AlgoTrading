# Auto KDJ Strategy

## Overview
The Auto KDJ strategy is a direct conversion of the MetaTrader 4 expert advisor `AutoKdj.mq4` created by *senlin ge*. The system trades a single symbol and evaluates the smoothed stochastic oscillator known as **KDJ** (also called %K, %D, %J). The StockSharp implementation recreates the same indicator logic and the money-management options exposed in the original expert advisor, while leveraging the high-level API features such as candle subscriptions, indicator binding, and automatic protective orders.

KDJ is built on top of the stochastic oscillator. It first computes a Raw Stochastic Value (RSV), smooths it into the %K line, smooths %K again into the %D line, and uses their difference (referred to as *KDC* in the source code) to detect shifts in momentum. Auto KDJ opens at most one market position at a time and immediately applies the requested stop-loss / take-profit protections.

## Indicator Construction
1. **RSV calculation** – For each finished candle the highest high and lowest low over `KDJ Length` candles are collected. RSV is calculated as:
   \[
   RSV = \frac{\text{Close} - \text{LowestLow}}{\text{HighestHigh} - \text{LowestLow}} \times 100
   \]
2. **%K smoothing** – RSV values are averaged over `Smooth %K` periods to obtain the %K line.
3. **%D smoothing** – %K values are averaged over `Smooth %D` periods to produce the %D line.
4. **KDJ signal** – The algorithm analyses `K - D` (the *KDC* buffer from the MQL version) and the slope of %K to generate entries and exits.

This pipeline is implemented with StockSharp's `Stochastic` indicator by configuring the period and smoothing parameters to mirror the MetaTrader buffers.

## Trading Rules
Signals are evaluated once per finished candle. The strategy refuses to open another position while there is an open trade or pending exit order, which matches the behaviour of the MQL expert advisor.

### Entry Conditions
- **Buy** when one of the following is true:
  - `K - D` crosses from negative to positive.
  - `K - D` is already positive and %K is rising (`K_current > K_previous`).
- **Sell** when one of the following is true:
  - `K - D` crosses from positive to negative.
  - `K - D` is already negative and %K is falling (`K_current < K_previous`).

### Exit Conditions
- **Close long** when `K - D` crosses below zero or when %K starts falling.
- **Close short** when `K - D` crosses above zero or when %K starts rising.

When the position is flattened, the strategy records whether the trade was profitable or not. Consecutive losses influence the next position size in exactly the same way as the `DecreaseFactor` logic from the MQL EA.

## Money Management
The original expert advisor provides a `whichmethod` switch to combine stop-loss and take-profit behaviour, plus a dynamic lot-size routine based on margin usage and loss streaks. The StockSharp port reproduces these capabilities as individual parameters:

- **Stop-loss / take-profit toggles** – Independent boolean flags allow enabling or disabling each protective leg. When active, `StartProtection` attaches the protective exits and handles market execution.
- **Risk-based volume** – The order size starts from `Base Volume` and can be increased to satisfy the requested `Maximum Risk` fraction of the portfolio. Margin consumption is approximated through the instrument contract size and configured leverage, which emulates the MT4 calculation `AccountFreeMargin * MaximumRisk * Leverage / 100000`.
- **Loss streak reduction** – After two or more consecutive losing trades the next order is reduced by `volume * losses / DecreaseFactor`, matching the original volume decay routine.

All volumes are normalised using the security's `VolumeStep`, `MinVolume`, and `MaxVolume` values to guarantee that the submitted order size is tradable.

## Parameters
| Parameter | Description | Default | Optimization |
|-----------|-------------|---------|--------------|
| **Candle Type** | Data type / timeframe of input candles. | 15-minute time frame | – |
| **KDJ Length** | Lookback period for RSV calculation. | 30 | 10 → 60 step 5 |
| **Smooth %K** | Smoothing applied to the %K line. | 3 | 1 → 10 step 1 |
| **Smooth %D** | Smoothing applied to the %D line. | 6 | 1 → 15 step 1 |
| **Stop Loss (pips)** | Distance for the protective stop. | 100 | 0 → 300 step 10 |
| **Take Profit (pips)** | Distance for the protective take-profit. | 200 | 0 → 400 step 10 |
| **Enable Stop Loss** | Toggle for the stop-loss leg. | Enabled | – |
| **Enable Take Profit** | Toggle for the take-profit leg. | Enabled | – |
| **Base Volume** | Minimal volume before risk adjustment. | 0.1 | – |
| **Maximum Risk** | Fraction of equity allocated per trade. | 0.4 | 0.0 → 1.0 step 0.1 |
| **Decrease Factor** | Volume reduction after loss streaks. | 0.3 | 0.0 → 5.0 step 0.5 |
| **Leverage** | Account leverage used in the margin model. | 100 | 10 → 500 step 10 |

## Usage Notes
1. Configure the desired security and connection in StockSharp Designer, Shell, or Runner.
2. Adjust the candle type to match the timeframe used in MetaTrader.
3. Set stop-loss / take-profit preferences through the boolean switches to reproduce the `whichmethod` behaviour:
   - Disable both legs for "no SL, no TP".
   - Enable only the take-profit or stop-loss leg to mirror the partial protection modes.
4. Optionally fine-tune `Base Volume`, `Maximum Risk`, `Decrease Factor`, and `Leverage` to mirror your broker configuration.
5. Start the strategy. The chart helper automatically plots candles, the KDJ indicator, and executed trades for verification.

## Differences Compared to the MQL Version
- The custom `kdj.mq4` indicator is replaced with StockSharp's built-in `Stochastic` indicator configured to provide identical buffers, removing the need for external files.
- Position sizing uses portfolio equity, contract size, and leverage supplied by the StockSharp security definition. Brokers with different contract multipliers can adjust `Base Volume` or `Maximum Risk` accordingly.
- Protective exits rely on `StartProtection`, which submits market orders when triggered and logs the fill price. This offers the same functional behaviour as the `OrderSend` + stop/take parameters in MetaTrader while remaining idiomatic to StockSharp.
- The risk reduction after consecutive losses is tracked through executed trades rather than scanning the entire trade history on every tick, improving performance while keeping the outcomes identical.

## Testing
The strategy was validated by comparing the generated entry/exit points against the original MQL logic on sample EURUSD data. Traders should still run walk-forward tests or optimisation in their target environment to confirm that the port behaves as expected with their broker's contract specifications and execution model.
