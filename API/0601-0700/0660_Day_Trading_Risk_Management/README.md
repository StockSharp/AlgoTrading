# Day Trading Strategy with Risk Management
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy enters long when the fast EMA crosses above the slow EMA while RSI is below the overbought level. It enters short when the fast EMA crosses below the slow EMA while RSI is above the oversold level. Risk management uses fixed take profit and stop loss percentages.

### Exact executed entry and protection rules

Defaults: `CandleType=1 minute`, `FastEmaPeriod=20`, `SlowEmaPeriod=50`, `RsiLength=14`, `RsiOverbought=70`, `RsiOversold=30`, `TakeProfitPercent=2`, `StopLossPercent=1`. On a **finished candle**, buy if prior `EMA20 <= EMA50` and current `EMA20 > EMA50` and `RSI14 < 70`; sell short if prior `EMA20 >= EMA50` and current `EMA20 < EMA50` and `RSI14 > 30`. Trades are allowed from flat and may reverse an opposing position using `Volume + abs(Position)`. A cross without RSI confirmation **cannot** open a trade. Filled positions are protected by **native fixed 2% take-profit and 1% stop-loss**, without trailing; these operate on live price updates independently of the bar-close signal. The prior 120/450 EMA-only crossover implementation did not satisfy the published RSI or protection contract. Numeric defaults were not supplied by the original README and are explicit implementation choices.



## Parameters
- Candle Type
- Fast EMA length
- Slow EMA length
- RSI length
- RSI Overbought
- RSI Oversold
- Take profit percent
- Stop loss percent
