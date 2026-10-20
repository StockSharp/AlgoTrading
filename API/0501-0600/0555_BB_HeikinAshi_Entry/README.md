# BB HeikinAshi Entry

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger reversal on Heikin Ashi candles with an executed half-position profit target and a trailing remainder.

## Trading rules

HA close is `(Open+High+Low+Close)/4`. The first HA open is `(Open+Close)/2`, subsequent HA opens are `(previous HA open+previous HA close)/2`. HA high/low include both HA open/close and the raw high/low. Bollinger uses HA closes from the first archived candle, not raw closes. A doji has neither color. Touches use `HA low <= lower` / `HA high >= upper`.

Exactly two or three consecutive bearish HA candles must each touch their own lower band; a bullish HA candle closing strictly above its lower band enters long. One candle or a streak of four or more does not qualify. Reverse colors, upper-band touches and a bearish close strictly below its upper band enter short.

Enter `Volume` only from flat. Initial stop is the previous completed raw candle's low for long, high for short. Freeze that level before the order; `R` is the positive distance from the weighted actual fill to that stop. At `1R` submit a market order for half the filled position, rounded down to `VolumeStep`. If either half is below `MinVolume`, close the whole position at the target. A fill beyond the initial stop is closed on the next executable quote.

Only an actual target fill activates trailing. The remaining position follows the latest completed raw candle low/high; the stop never loosens. Executable bid (long) or ask (short) triggers full stop exits between candles, with stop priority over target. Pending orders block duplicate submissions. Earlier intrabar extremes do not trigger levels created later. There is no fixed percentage target or hidden cooldown.

Only finished candles and fully formed indicators can generate entries. An existing position is managed until its exit; pending market orders prevent duplicate submissions.

Bollinger deviation is the population standard deviation of the rolling closes, including the current finished candle.

## Parameters

| Parameter | Default |
|---|---|
| `BollingerLength` | `20` |
| `BollingerWidth` | `2` |
| `CandleType` | `15m` |
