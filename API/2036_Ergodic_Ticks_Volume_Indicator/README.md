# Ergodic Ticks Volume Indicator Strategy

This strategy applies the True Strength Index (TSI) to candle data and compares it with an exponential moving average signal line. A long position is opened when the TSI crosses above the signal line, while a short position is opened when it crosses below.

## Parameters

- **Candle Type** – timeframe of candles used for calculations.
- **Short Length** – fast smoothing period of TSI.
- **Long Length** – slow smoothing period of TSI.
- **Signal Length** – period of the EMA used as the signal line.

## Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate the TSI for each finished candle.
3. Process the TSI through an EMA to obtain a signal line.
4. When the TSI crosses above the signal line, enter long (closing any short position).
5. When the TSI crosses below the signal line, enter short (closing any long position).

The strategy is an adaptation of the MQL example "exp_ergodic_ticks_volume_indicator.mq5" and uses only built-in StockSharp indicators.
