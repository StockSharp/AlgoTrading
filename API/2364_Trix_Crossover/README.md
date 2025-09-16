# TRIX Crossover Strategy

This strategy uses two TRIX (Triple Exponential Moving Average Oscillator) indicators with different periods to detect potential reversals. A long position is opened when the fast TRIX forms a local bottom while the slow TRIX is rising. A short position is opened when the fast TRIX forms a local top while the slow TRIX is falling.

## Parameters

- **Fast TRIX Period** – period of the fast TRIX indicator.
- **Slow TRIX Period** – period of the slow TRIX indicator.
- **Take Profit** – profit target in absolute price units.
- **Stop Loss** – maximum loss in absolute price units.
- **Candle Type** – timeframe or data type for candles.

## Trading Logic

1. Subscribe to the selected candle type.
2. Compute fast and slow TRIX values on each finished candle.
3. Enter long when the fast TRIX value is higher than its previous value, the previous value is lower than the value before it, and the slow TRIX is rising.
4. Enter short when the fast TRIX value is lower than its previous value, the previous value is higher than the value before it, and the slow TRIX is falling.
5. Only one position is held at a time.
6. Stop loss and take profit protections are applied automatically.

## Notes

The strategy is an adaptation of an MQL5 script and demonstrates how to work with TRIX indicators within StockSharp.
