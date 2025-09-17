# Expert Alligator Strategy

The **Expert Alligator Strategy** is a faithful StockSharp port of MetaTrader 5's built-in expert advisor `Expert_Alligator.mq5`. The original expert drives its trading decisions from Bill Williams' Alligator indicator, which consists of three smoothed moving averages shifted into the future: the jaw (blue), the teeth (red), and the lips (green). By monitoring how these lines contract and expand the EA identifies fresh crossovers and waits for the "mouth" to open before another trade can be taken. This C# conversion recreates the same workflow with StockSharp's high-level strategy API and indicator suite.

## Trading logic

1. **Indicator preparation**
   - Build three smoothed moving averages of the median price using the classic Alligator lengths (13, 8, and 5) and apply the standard forward shifts (8, 5, and 3 bars respectively).
   - Store a rolling history of each shifted line so that past and future offsets used by the MetaTrader EA (e.g. `LipsTeethDiff(-2)`) can be evaluated safely.

2. **Entry conditions**
   - *Long trades*: trigger when the lips-teeth and teeth-jaw spreads have been shrinking for three consecutive shifted bars while remaining above zero. This reproduces the EA's requirement that the green line crosses downward through the red, confirming an upward mouth opening.
   - *Short trades*: mirror the long logic with spreads tightening below zero, indicating the lips crossing upward through the teeth and jaw.
   - After a trade is opened the strategy raises an internal `crossed` flag that blocks additional entries until the three Alligator spreads widen by at least the configured **Cross Measure** distance.

3. **Exit conditions**
   - *Long positions* close when the lips-teeth spread flips negative on the most recent shifted value while staying positive on the two older values (`-1`, `0`, `1` indexes in the original EA).
   - *Short positions* exit when the same sequence occurs in the opposite direction.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `Order Volume` | Trade size in lots or contracts passed to `BuyMarket`/`SellMarket`. | `0.1` |
| `Candle Type` | Time frame for the candle subscription. | `1 Hour` |
| `Jaw Period` | Smoothed moving average length for the jaw line. | `13` |
| `Jaw Shift` | Forward displacement (in bars) of the jaw line. | `8` |
| `Teeth Period` | Smoothed moving average length for the teeth line. | `8` |
| `Teeth Shift` | Forward displacement (in bars) of the teeth line. | `5` |
| `Lips Period` | Smoothed moving average length for the lips line. | `5` |
| `Lips Shift` | Forward displacement (in bars) of the lips line. | `3` |
| `Cross Measure` | Minimum spread (in MetaTrader points) that must develop after a crossover before another trade can fire. | `5` |

## Implementation notes

- The strategy calculates the median price `(High + Low) / 2` for each finished candle and feeds it into three `SmoothedMovingAverage` instances.
- Shifted histories are implemented with fixed-size arrays to mirror the way MetaTrader exposes future indexes like `-1` or `-2` once the Alligator lines are displaced forward.
- The MetaTrader `_Point` value is emulated through the symbol's `PriceStep`. When the latter is unavailable the code falls back to `10^-Decimals` or `0.0001`.
- The chart output matches the EA by plotting the jaw, teeth, and lips on the primary candle panel, allowing quick visual validation.

## Usage

1. Attach the strategy to a `Connector` with a security that provides the desired candle type (default one-hour candles).
2. Call `Start()` once the market data stream is ready.
3. Optional: adjust the Alligator lengths, shifts, or the cross measure threshold to test custom behaviours.
4. Monitor positions and performance through the standard StockSharp interfaces.

No additional trailing stops or money-management modules are required because the original EA uses fixed lot sizing and relies solely on the Alligator line geometry for trade management.
