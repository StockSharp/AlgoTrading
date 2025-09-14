# Cho With Flat Strategy

This strategy trades based on the crossover of the **Chaikin Oscillator** and its moving average. A Bollinger Bands filter is used to avoid trading during flat markets.

## Parameters
- **Candle Type** – timeframe of input candles.
- **Fast Period** – fast period of the Chaikin Oscillator.
- **Slow Period** – slow period of the Chaikin Oscillator.
- **MA Period** – period of the moving average applied to the oscillator.
- **MA Type** – moving average type for the signal line.
- **Bollinger Period** – period of the Bollinger Bands.
- **Std Deviation** – standard deviation for the Bollinger Bands.
- **Flat Threshold** – minimal band width (in points) to consider market active.

## Trading Logic
1. Calculate Chaikin Oscillator and its moving average.
2. Build Bollinger Bands on price for flat market detection.
3. Skip trades if the Bollinger band width is below `Flat Threshold`.
4. **Buy** when the oscillator crosses below its signal line.
5. **Sell** when the oscillator crosses above its signal line.

The position direction always follows the latest crossover while the flat filter prevents trading in sideways market conditions.
