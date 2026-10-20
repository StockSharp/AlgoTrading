# Bitcoin 1H/15M Breakout

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

Trade finished 15-minute breakouts of the preceding fully completed one-hour range. Lower-frame candles are aggregated into UTC hour buckets. An hour is valid only when all four consecutive quarters from its exact opening are present; an initial partial hour or a data gap cannot create a range.

For a lower candle opening inside hour H, use only the high/low of completed hour H−1. Never use a partially built current hour. While flat, buy `Volume` if the lower candle closes strictly above that high, or sell `Volume` strictly below that low. Both directions are allowed; open positions do not add or reverse on breakout signals. An active order blocks another submission.

Native local protection sets `StopLossBuffer` as an absolute price distance from actual entry fills, default 50. The take-profit distance is `StopLossBuffer * RiskRewardRatio`, default 100 for ratio 2. Both are market exits of the entire position; levels do not drift with later hourly ranges. `HigherTimeFrame` must be a whole multiple of the lower interval.

Native local protection uses the instrument's candle and subscribed price updates. Distances are anchored to actual fills. Exits are market orders; execution prices depend on the available market and price grid.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | `15m` |
| `HigherTimeFrame` | `1h` |
| `StopLossBuffer` | `50` |
| `RiskRewardRatio` | `2` |
