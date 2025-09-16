# Above Below MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Above Below MA strategy mirrors the MetaTrader expert advisor *Above Below MA (barabashkakvn's edition)*. It monitors how far current prices trade relative to a configurable moving average and allows trades only when price is on the "wrong" side of the average by at least a defined distance while the average itself trends in the anticipated direction. The logic has been ported to the StockSharp high level API and executes exclusively on completed candles.

## Overview

- **Market Regime**: Works best on instruments that frequently retest a moving average before resuming the trend.
- **Instruments**: Any instrument supported by your StockSharp connection. Forex pairs benefit the most because the original script measured distance in pips.
- **Timeframe**: Adjustable through the *Candle Type* parameter (default 1-minute time frame).
- **Position Direction**: Both long and short trades are supported, but only one net position can exist at any given time.

## Strategy Logic

1. Calculate a moving average on the selected candle series. The averaging method (SMA, EMA, SMMA, WMA), applied price (close, open, high, low, median, typical, weighted) and forward shift replicate the MetaTrader inputs.
2. Convert the minimum distance expressed in pips into an actual price offset using the instrument's `PriceStep`. If the broker does not publish a price step, the distance filter is skipped automatically.
3. On each finished candle:
   - **Long setup**:
     - Candle open and close must lie at least the configured distance below the shifted moving average.
     - The moving average must be rising compared with the previous candle.
   - **Short setup**:
     - Candle open and close must lie at least the configured distance above the shifted moving average.
     - The moving average must be falling compared with the previous candle.
4. The strategy closes any opposite position before sending a new market order in the signal direction. No simultaneous long/short exposure is allowed.

All trading decisions are made on completed candles to avoid repeated entries inside a forming bar. Orders are executed via `BuyMarket` or `SellMarket` with the configured volume.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `MaPeriod` | Moving average length. Default 6.
| `MaShift` | Number of candles to shift the moving average forward. A value of 0 uses the current bar, `n` uses the value from `n` bars ago. Default 0.
| `MaMethod` | Moving average type: `Simple`, `Exponential`, `Smoothed`, or `Weighted`. Default `Exponential`.
| `AppliedPrice` | Price source: close, open, high, low, median, typical, or weighted. Default `Typical`.
| `MinimumDistancePips` | Required distance in pips between candle prices and the moving average. Converted using `PriceStep`. Default 5.
| `CandleType` | Candle type driving indicator updates. Default 1-minute time frame.
| `TradeVolume` | Order volume for new entries. Default 1.

## Additional Notes

- No stop-loss or take-profit logic is included. Risk management must be implemented via portfolio settings or external modules.
- The moving average shift buffer is kept minimal and respects the "no collections" guideline by storing only the values required for the specified shift.
- When `PriceStep` is unavailable the minimum distance filter cannot be evaluated, so entries rely solely on the moving average conditions.
- The strategy draws the candle series, the moving average indicator, and your trades on the default chart area when a chart container is available.
