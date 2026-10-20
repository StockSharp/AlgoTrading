# BBTrend SuperTrend Decision

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

Compute two Bollinger envelopes on finished closes (20 and 50 bars, two population standard deviations). With short/long lower bands `Ls/Ll`, upper bands `Us/Ul` and short mean `Ms`, `BBTrend = 100 * (abs(Ls-Ll) - abs(Us-Ul)) / Ms`.

Apply SuperTrend to this score, using the previous/current score as synthetic open/close and their minimum/maximum as low/high. True range is the absolute score change; initialize ATR with ten ranges, then use Wilder smoothing. Bands use the synthetic midpoint and `7 * ATR`, trailing the previous bands until broken. Trading waits for both envelopes and ATR to form.

An upward SuperTrend targets `+Volume`, a downward one `-Volume`; an opposite signal trades the difference and fully reverses the position. `TradeDirection = Buy/Sell` permits entries only on that side; the opposite signal still closes the existing position. `null` permits both sides. Pending orders block another submission.

`TpSlCondition` selects `None`, `TP`, `SL` or `Both` (Python ordinals 0/1/2/3). Protection is disabled by default. Enabled distances use `TakeProfitPerc` or `StopLossPerc` percent; they are not absolute price points.

Native local protection uses the instrument's candle and subscribed price updates. Distances are anchored to actual fills. Exits are market orders; execution prices depend on the available market and price grid.

## Parameters

| Parameter | Default |
|---|---|
| `ShortBbLength` | `20` |
| `LongBbLength` | `50` |
| `StdDev` | `2` |
| `SupertrendLength` | `10` |
| `SupertrendMultiplier` | `7` |
| `TradeDirection` | `null` |
| `TpSlCondition` | `None` |
| `TakeProfitPerc` | `30` |
| `StopLossPerc` | `20` |
| `CandleType` | `5m` |
