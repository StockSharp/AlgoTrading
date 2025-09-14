# Color XCCX Candle Strategy

Converted from MQL code `MQL/14260`.

This strategy compares two simple moving averages (SMA) built from candle open and close prices. When the SMA calculated from close prices crosses above the SMA based on open prices, a long position is opened. When the close-based SMA crosses below the open-based SMA, a short position is opened. Any existing opposite position is closed before opening a new one.

Parameters:

- `SMA Length` – number of candles used to calculate both SMAs.
- `Candle Type` – timeframe for incoming candles.
- `Stop Loss %` – stop loss size as a percent of entry price.
- `Take Profit %` – take profit size as a percent of entry price.

The strategy uses the high-level StockSharp API to subscribe to candles and bind indicators. It also plots both SMAs and executed trades on the chart when visualization is available.
