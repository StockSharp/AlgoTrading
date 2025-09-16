# Lego 4 Beta Strategy

This strategy is a modular system translated from the MetaTrader script "exp_Lego_4_Beta". It combines several common technical indicators and allows enabling or disabling each component through parameters.

## Algorithm

1. **Moving Average Cross** – A fast and a slow moving average are calculated. A long position opens when the fast average crosses above the slow average. A short position opens on the opposite cross.
2. **Stochastic Oscillator Filter** – When enabled, long entries require the Stochastic %K value to be below the oversold level, and short entries require %K to be above the overbought level.
3. **RSI Exit** – When enabled, existing long positions are closed if RSI rises above the high threshold. Short positions are closed when RSI drops below the low threshold.

## Parameters

- `UseMaOpen` – activate moving average cross signals.
- `FastMaLength` / `SlowMaLength` – lengths of the fast and slow moving averages.
- `MaType` – type of moving average (SMA, EMA, WMA).
- `UseStochasticOpen` – enable Stochastic filter for entries.
- `StochLength` – main period for Stochastic calculation.
- `StochKPeriod` / `StochDPeriod` – smoothing periods for %K and %D lines.
- `StochBuyLevel` / `StochSellLevel` – oversold and overbought thresholds.
- `UseRsiClose` – enable RSI-based exits.
- `RsiPeriod` – RSI calculation length.
- `RsiHigh` / `RsiLow` – RSI thresholds for closing positions.
- `CandleType` – candle type to subscribe.

## Notes

The strategy uses high level `SubscribeCandles` with `BindEx` to process indicator values and follows the StockSharp recommended style. Only market orders are used for entry and exit.
