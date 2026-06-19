# The MasterMind 策略

该策略利用随机震荡指标(Stochastic)和威廉指标(Williams %R)来捕捉极端的超买和超卖情况。

## 概述
策略监控两个动量指标：
- **Stochastic Oscillator** 基础周期为100，平滑参数为3/3。
- **Williams %R** 周期为100。

当 Stochastic 的 %D 值低于 3 且 Williams %R 低于 -99.9 时，认为市场超卖，开多头。
当 %D 高于 97 且 Williams %R 高于 -0.1 时，认为市场超买，开空头。

进场后通过止损、止盈、跟踪止损以及可选的保本位管理风险。

## 参数
- `StochasticLength` – Stochastic 和 Williams %R 的计算周期。
- `StopLoss` – 止损距离（点）。
- `TakeProfit` – 止盈距离（点）。
- `TrailingStop` – 启动跟踪止损的距离（点）。
- `TrailingStep` – 跟踪止损的步长（点）。
- `BreakEven` – 当盈利达到该值时将止损移至入场价（点）。
- `CandleType` – 用于计算的K线周期。

## 指标
- `StochasticOscillator`
- `WilliamsR`

## 交易规则
1. 当 `%D < 3` 且 `Williams %R < -99.9` 时买入。
2. 当 `%D > 97` 且 `Williams %R > -0.1` 时卖出。
3. 入场后应用止损和止盈。
4. 当利润达到 `BreakEven` 时，将止损移至入场价。
5. 当价格移动 `TrailingStop` 后，按 `TrailingStep` 跟踪止损。

## 备注
本策略使用 StockSharp 的高级 API，主要用于学习示例。
