# Dema RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades crossovers between the RSI of a double exponential moving average and its smoothed value. A long position opens when the RSI crosses above the smoothed line and a short position opens on the opposite crossover. Positions can be protected with take profit, trailing stop and an optional trading session filter.

## Parameters
- Candle Type
- MA length
- RSI length
- RSI smoothing length
- Take profit points
- Trail stop points
- Use session
- Session start
- Session end

### Executed RSI(DEMA) contract

Defaults: `CandleType=1m`, `MaLength=20`, `RsiLength=14`, `RsiSmoothingLength=5`, `TakeProfitPoints=0`, `TrailStopPoints=0`, `UseSession=false`, `SessionStart=09:00`, `SessionEnd=17:00`. For each finished candle calculate `EMA1=EMA(Close,20)`, `EMA2=EMA(EMA1,20)`, `DEMA=2×EMA1−EMA2`, `R=RSI(DEMA,14)`, `S=SMA(R,5)`. Buy when the **previous** `R <= S` and **current** `R > S`, if flat or short. Sell when previous `R >= S` and current `R < S`, if flat or long. A reversal sizes the order `Volume + abs(Position)`. Optional `UseSession=true` applies the entry filter to the half-open interval `[SessionStart,SessionEnd)`; overnight intervals are supported and protection exits are not time-restricted. `TakeProfitPoints` and `TrailStopPoints` are **instrument minimum-price ticks**: nonzero settings use native absolute-distance take-profit and trailing stop, zero disables that leg. The original README gave no numerical defaults; these values are explicit implementation choices. The old 120/450 price-EMA crossover was unrelated to this indicator.

