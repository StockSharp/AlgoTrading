# Color XdinMA with Standard Deviation Strategy

This strategy is a StockSharp port of the MQL5 expert **Exp_ColorXdinMA_StDev**.
It combines two moving averages into a single line named `XdinMA` and tracks its
change over time. The difference between the current and the previous `XdinMA`
value is compared against a multiple of its recent standard deviation. When the
change exceeds the positive threshold a long position is opened, while a move
below the negative threshold opens a short position.

## How it works

1. Two simple moving averages are calculated:
   - **Main MA** – period defined by `MainLength`.
   - **Plus MA** – period defined by `PlusLength`.
2. The custom line `XdinMA = 2 * MainMA - PlusMA` is built.
3. The change of `XdinMA` between consecutive candles is passed to a standard
   deviation indicator with length `StdPeriod`.
4. If the change is greater than `K1 * StdDev`, a buy order is placed. If it is
   smaller than `-K1 * StdDev`, a sell order is placed. Existing opposite
   positions are closed before opening a new one.

## Parameters

| Parameter   | Description                                        |
|-------------|----------------------------------------------------|
| `MainLength`| Period for the primary moving average.             |
| `PlusLength`| Period for the secondary moving average.           |
| `StdPeriod` | Number of bars used for standard deviation.        |
| `K1`        | Multiplier for the deviation threshold.            |
| `K2`        | Reserved for future extension of the second filter.|

All parameters are exposed through `StrategyParam` so they can be optimized or
changed from the user interface.

## Notes

- Only completed candles are processed.
- The strategy uses market orders and does not implement stop-loss or
  take-profit logic.
- Chart drawing includes both moving averages and executed trades for visual
  analysis.
