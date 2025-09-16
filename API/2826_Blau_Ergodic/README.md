# Blau Ergodic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **Exp_BlauErgodic** expert advisor from MQL5 to StockSharp. It rebuilds the Blau Ergodic oscillator by
triple-smoothing the momentum and its absolute value with EMA filters, generates a normalized oscillator and a signal line, and
offers three distinct signal modes that mirror the original EA.

The default configuration evaluates completed 4-hour candles. You can change the applied price (close, open, high/low-based
averages), every smoothing depth, and the bar index (`SignalBar`) used to read signals. Trades are sized via the strategy's
`Volume` property; long/short entries or exits can be disabled individually through boolean parameters. Protective stop-loss and
take-profit levels are defined in points and are converted into absolute prices through `Security.PriceStep`.

## Signal modes

- **Breakdown** – reacts to the oscillator crossing the zero line. Longs open on negative-to-positive flips and shorts on
  positive-to-negative flips. Positions are closed when the oscillator remains on the opposite side of zero.
- **Twist** – searches for slope reversals. A long setup appears when the oscillator was falling on the prior bar but turns up on
  the most recent bar; a short setup requires the inverse pattern.
- **CloudTwist** – monitors the oscillator crossing its signal line. Longs trigger when the oscillator rises through the signal
  cloud, and shorts when it falls back below it.

All modes read indicator values from the bar specified by `SignalBar` (default `1`, meaning the last completed bar) and rely on
older values for confirmation. Set `SignalBar` to at least `1` because the conversion processes finished candles only.

## Entry and exit rules

- **Long entries:** enabled when `AllowBuyEntry` is true, no existing long position is open (`Position <= 0`), and the active mode
  generates a buy condition. The strategy reverses any short exposure by buying `Volume + |Position|`.
- **Short entries:** enabled when `AllowSellEntry` is true, no existing short position is open (`Position >= 0`), and the active
  mode issues a sell condition. It covers any long exposure before establishing the short.
- **Long exits:** triggered by the mode-specific condition, or whenever `StopLossPoints` / `TakeProfitPoints` are reached. Forced
  exits bypass the `AllowBuyExit` flag so protective stops are always honored.
- **Short exits:** analogous to the long exit logic with `AllowSellExit` and stop levels for short trades.

## Parameters

- `CandleType` – timeframe for candle subscriptions (default 4-hour candles).
- `Mode` – one of `Breakdown`, `Twist`, or `CloudTwist`.
- `MomentumLength` – lookback for the raw momentum difference.
- `First/Second/ThirdSmoothingLength` – EMA depths for the cascading momentum filters.
- `SignalSmoothingLength` – EMA depth for the signal line.
- `SignalBar` – index of the completed bar used to read signals (minimum `1`).
- `AppliedPrice` – price source feeding the oscillator (close, open, median, typical, weighted, etc.).
- `AllowBuyEntry`, `AllowSellEntry`, `AllowBuyExit`, `AllowSellExit` – enable or disable specific operations.
- `StopLossPoints`, `TakeProfitPoints` – protective distances in points (converted via `Security.PriceStep`).

The conversion maintains the behaviour of the MQL5 expert, while leveraging the StockSharp high-level API (`SubscribeCandles`,
`Bind`) and adhering to StockSharp strategy conventions with tab indentation and English comments.
