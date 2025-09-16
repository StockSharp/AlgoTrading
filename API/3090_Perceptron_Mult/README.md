# Perceptron Mult Strategy

This strategy ports the **Peceptron_Mult.mq5** expert advisor to the StockSharp high-level API. It simultaneously monitors up to three independent markets and applies the Acceleration/Deceleration (AC) oscillator inside a perceptron model. Each market receives its own weight configuration, position sizing, and protective exits so the behaviour of the original multi-symbol advisor is preserved.

## Trading Logic

1. For every configured security the strategy subscribes to the same candle type (default: 1-minute).
2. On each finished candle it calculates the Bill Williams Acceleration/Deceleration oscillator:
   - Compute the Awesome Oscillator (AO) from candle highs and lows (5/34 median price moving averages).
   - Subtract a 5-period simple moving average of AO from the current AO value.
3. A rolling buffer with the latest 22 AC values is maintained per security.
4. The perceptron signal is formed from four delayed AC values using weights (`w - 100`) exactly as in the MQL code:
   - `AC[0]`, `AC[7]`, `AC[14]`, `AC[21]` correspond to the most recent and three historical readings.
5. Entry rules:
   - Positive sum ⇒ open a long position if no position exists on that security.
   - Negative sum ⇒ open a short position if the security is flat.
6. Exit rules:
   - Stop-loss and take-profit distances are expressed in points. They are converted to absolute price offsets using the instrument price step.
   - Protective exits are evaluated on every finished candle. A long trade is closed when the candle low hits the stop or the high reaches the profit target; shorts use the mirrored logic.
7. Positions are mutually exclusive per security. The strategy ignores new signals while exposure remains open, replicating the original advisor behaviour.

## Parameters

| Parameter | Description |
| --- | --- |
| `FirstSecurity`, `SecondSecurity`, `ThirdSecurity` | Instruments processed by the perceptron. Leave `null` to disable a slot.
| `FirstOrderVolume`, `SecondOrderVolume`, `ThirdOrderVolume` | Market order size for each instrument.
| `FirstWeight1`…`FirstWeight4`, etc. | Perceptron weights (MQL inputs `x1…x12`). The strategy internally subtracts 100 from each value before applying it.
| `FirstStopLossPoints`, `SecondStopLossPoints`, `ThirdStopLossPoints` | Stop-loss distance in price points for each instrument. Set to 0 to disable.
| `FirstTakeProfitPoints`, `SecondTakeProfitPoints`, `ThirdTakeProfitPoints` | Take-profit distance in price points for each instrument. Set to 0 to disable.
| `CandleType` | Candle series shared by all securities.

## Implementation Notes

- The strategy relies on `AwesomeOscillator` and `SimpleMovingAverage` indicators from StockSharp to reconstruct the AC oscillator, avoiding manual recalculations.
- Rolling buffers are used only to emulate the perceptron inputs from the MQL implementation (indices 0, 7, 14, 21).
- Protective levels are enforced without registering separate stop orders: the strategy monitors candle extremes and closes positions with market orders when levels are breached, mirroring the behaviour of the original EA on new ticks.
- Each security maintains independent indicator state, order volume, and risk settings, matching the three-symbol structure of the source advisor.

## Usage Tips

1. Assign up to three securities in the parameter panel. Any unused slot can remain `null`.
2. Adjust the point-based stops and targets to match the tick size of the selected instruments.
3. Tune the perceptron weights to emphasise specific lags of the AC oscillator if optimisation is required.
4. Because all instruments share the same candle type, ensure historical data is available for every configured security.
