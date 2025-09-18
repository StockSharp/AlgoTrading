# GBPCHF Correlation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **GBPCHF Correlation Strategy** replicates the MetaTrader expert "GbpChf 4" using the StockSharp high-level API. It observes how GBPUSD and USDCHF move relative to each other on hourly candles. When both currency legs agree on bullish or bearish momentum, the strategy opens a position on the configured instrument (typically GBPCHF).

## How It Works
- Builds two MACD indicators (12/26/9) on GBPUSD and USDCHF hourly candles.
- Evaluates both the MACD histogram (momentum) and the signal line (trend confirmation).
- A long signal appears when both histograms are above zero, the GBPUSD histogram is weaker than USDCHF, and the GBPUSD signal line is higher than the USDCHF signal line.
- A short signal requires both histograms below zero, the GBPUSD histogram stronger on the downside than USDCHF, and the GBPUSD signal line lower than the USDCHF signal line.
- Only one order per direction is generated per candle. Optional net-position restriction keeps a single open position at all times.

## Risk Management
- Stop-loss and take-profit distances are expressed in pips and converted to absolute price steps using the traded instrument's `PriceStep`.
- Automatic protection is started through `StartProtection`, so the broker/server manages exits even when the strategy is offline.

## Parameters
- `Volume` – Trade size per signal. Default `0.01`.
- `StopLossPips` – Protective stop distance in pips. Default `90`.
- `TakeProfitPips` – Profit target distance in pips. Default `45`.
- `OnlyOnePosition` – When `true`, net exposure must be flat before opening a new trade.
- `FastPeriod` – Fast EMA length for MACD. Default `12`.
- `SlowPeriod` – Slow EMA length for MACD. Default `26`.
- `SignalPeriod` – Signal SMA length for MACD. Default `9`.
- `CandleType` – Timeframe used for all subscriptions. Default `1h` candles.
- `GbpUsdSymbol` – Identifier used to resolve the GBPUSD security.
- `UsdChfSymbol` – Identifier used to resolve the USDCHF security.

## Trading Notes
- Works best when GBPUSD and USDCHF data feeds are synchronized and provide complete hourly candles.
- Cancelled and filled orders are logged through the base strategy, so monitoring the log output helps verify correlation behavior.
- The strategy cancels standing orders before submitting a new market order, ensuring clean position flips.
- Requires that the connector knows how to resolve the GBPUSD and USDCHF instruments (for example via symbol mapping or security lookup).

## Defaults Summary
- **Timeframe**: Hourly candles.
- **Direction**: Long and short.
- **Instruments**: Trades the configured `Security` (GBPCHF by default), analyses GBPUSD and USDCHF.
- **Stops**: Yes (fixed pip distances).
- **Indicators**: MACD histogram and signal line on GBPUSD and USDCHF.
- **Complexity**: Intermediate (multi-instrument correlation with stop management).
