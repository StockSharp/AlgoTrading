# BB RSI Trailing Stop

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger excursions require RSI recovery, followed by a conditional trailing stop. `p` is the previous formed candle; RSI uses Wilder smoothing.

## Trading rules

Long: raw `Low < lowerBand` and `RSI[p] <= RsiOversold && RSI > RsiOversold`. Short: raw `High > upperBand` and `RSI[p] >= RsiOverbought && RSI < RsiOverbought`. Still oversold/overbought is insufficient. Enter `Volume` only from flat; there is no hidden bar cooldown.

Use the weighted actual entry fill and executable bid for a long, ask for a short. Initial stop distance is `StopLossPoints`. Track the favorable quote extreme; trailing activates only after `TrailOffsetPoints` favorable movement. Afterwards the stop follows that extreme at `TrailStopPoints`, never loosening the initial stop. Quote crossing closes the complete filled position by market order. All three distances are absolute price units, not ticks or percentages. No candle high/low is reused to trigger a stop created later in that candle.

Only finished candles and fully formed indicators can generate entries. An existing position is managed until its exit; pending market orders prevent duplicate submissions.

Bollinger deviation is the population standard deviation of the rolling closes, including the current finished candle.

## Parameters

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `25` |
| `BollingerDeviation` | `2` |
| `RsiPeriod` | `14` |
| `RsiOversold` | `33` |
| `RsiOverbought` | `60` |
| `StopLossPoints` | `50` |
| `TrailOffsetPoints` | `99` |
| `TrailStopPoints` | `40` |
| `CandleType` | `15m` |
