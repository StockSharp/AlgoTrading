# ColorJFatl StDev Strategy

This strategy is a translation of the **ColorJFatl_StDev** expert advisor from MQL5 into the StockSharp API. It combines the Jurik Moving Average (JMA) with standard deviation bands to generate trading signals.

## Strategy Logic

1. Calculate the JMA on closing prices.
2. Compute the standard deviation over a configurable period.
3. Build two sets of dynamic bands using multipliers `K1` and `K2`:
   - `upper1 = JMA + K1 * StdDev`
   - `upper2 = JMA + K2 * StdDev`
   - `lower1 = JMA - K1 * StdDev`
   - `lower2 = JMA - K2 * StdDev`
4. Depending on the selected signal mode, the strategy opens or closes positions:
   - **Point** – triggers when price crosses the bands.
   - **Direct** – uses turning points of the JMA line.
   - **Without** – disables the corresponding signal.

## Parameters

| Name | Description |
|------|-------------|
| `CandleTimeFrame` | Time frame for candle data. |
| `JmaLength` | Period of the Jurik Moving Average. |
| `JmaPhase` | Phase for the JMA calculation. |
| `StdPeriod` | Period for standard deviation. |
| `K1` | First deviation multiplier. |
| `K2` | Second deviation multiplier. |
| `BuyOpenMode` | Mode for opening long positions. |
| `SellOpenMode` | Mode for opening short positions. |
| `BuyCloseMode` | Mode for closing long positions. |
| `SellCloseMode` | Mode for closing short positions. |

## Usage

The strategy subscribes to candles of the specified time frame, processes JMA and standard deviation values, and automatically submits market orders based on the defined modes.

This implementation focuses on clarity and can serve as a starting point for further enhancements or custom risk management.

