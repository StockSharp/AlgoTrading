# Aussie Surfer Ltd Strategy

## Overview
The **Aussie Surfer Ltd Strategy** is a StockSharp high level API port of the MetaTrader 5 expert advisor "Aussie Surfer Ltd" (MQL folder `43278`). The strategy mixes fast Bollinger Band reversals with an Alligator trend filter to automate the discretionary setup used in the original EA. Trades are taken on the primary instrument configured for the strategy and evaluated on a 15-minute candle series by default.

## Indicators and Data
- **Bollinger Bands (Close price, default length 5, width 2.5)** – detect when the market temporarily stretches outside of the bands and snaps back inside.
- **Smoothed Moving Average (length 21)** – reproduces the Alligator "teeth" line to judge trend deceleration.
- **Median price of each candle ((High + Low) / 2)** – feeds the Alligator calculation so that the slope matches the original implementation.

The strategy subscribes to a single candle stream. Indicator values are driven by finished candles only, ensuring that signals are generated on confirmed data.

## Trading Logic
1. **Entry setup**
   - When the previous candle opened above the lower Bollinger Band and the current candle opens below the band value observed two bars ago, a **long** position is opened (after flattening any short exposure). This recreates the EA logic where the price pierces the lower band and immediately bounces back inside.
   - When the previous candle opened below the upper Bollinger Band and the current candle opens above the band value observed two bars ago, a **short** position is opened (after flattening any long exposure).
2. **Alligator-based exit**
   - The Alligator teeth line is monitored one and two bars back. A long position is liquidated whenever the slope turns downward (the value two bars ago is greater than the value one bar ago). A short position closes when the slope turns upward.
3. **Risk layers**
   - A fixed pip stop-loss and take-profit are applied on entry. Both are optional and can be disabled by setting their pip distance to zero.
   - An optional trailing stop re-aligns the stop-loss with the high (for longs) or low (for shorts) of the previously completed candle minus/plus the configured pip distance. The trailing logic is only active if the stop-loss is enabled and `EnableTrailingStop` is set to `true`.

## Risk Management
- **Stop-loss** – converts the configured pip distance into price units using the security price step.
- **Take-profit** – computed once on entry and kept static until either reached or the position is closed by another rule.
- **Trailing stop** – advances the stop-loss when a more favorable high (for longs) or low (for shorts) appears on the prior candle.
- **Reversal handling** – if a signal arrives while an opposite position is open, the strategy sends a market order sized to fully reverse and establish the new exposure in a single transaction.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base trade size in lots or contracts. | `0.30` |
| `StopLossPips` | Protective stop distance in pips. `0` disables the stop. | `46` |
| `TakeProfitPips` | Profit target distance in pips. `0` disables the target. | `0` |
| `EnableTrailingStop` | Enables pip-based trailing when a stop-loss is active. | `true` |
| `BollingerPeriod` | Length of the Bollinger Bands window. | `5` |
| `BollingerDeviation` | Standard deviation multiplier for the bands. | `2.5` |
| `TeethPeriod` | Smoothed moving average length for the Alligator teeth line. | `21` |
| `CandleType` | Candle series used for calculations (15-minute timeframe by default). | `15m` candles |

All numeric parameters include optimization metadata so they can be tuned via the Strategy Analyzer.

## Implementation Notes
- Only completed candles are processed; unfinished data is ignored to mimic the MetaTrader timer-driven execution that ran at the start of each new bar.
- Trailing logic requires a positive stop-loss distance. An exception is thrown during initialization if the trailing option is enabled without a stop.
- Indicator instances are drawn automatically when a chart area is available, helping validate that the StockSharp port matches the MetaTrader template.

## Usage
1. Load the strategy into a StockSharp terminal or backtesting environment.
2. Configure the trading security and adjust the parameters (especially pip distances) to match the broker's contract specifications.
3. Start the strategy. It will subscribe to the configured candle series, evaluate entries on each finished candle, and manage the position using the described rules.

For live trading, make sure the broker supports market orders and that the symbol's `PriceStep` is available so pip conversions are accurate.
