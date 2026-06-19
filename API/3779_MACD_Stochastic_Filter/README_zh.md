# MACD + 随机趋势过滤策略
[English](README.md) | [Русский](README_ru.md)

本策略重现 `MQL/7604` 目录中的 MetaTrader 智能交易系统。原脚本调用了一个返回“绿色/红色”缓冲区的自定义指标。参数组合 `(15, 3, 3)` 与经典随机指标完全一致，因此在 StockSharp 版本中我们使用平台自带的 `Stochastic` 作为过滤器，同时辅以 MACD 与指数移动平均线 (EMA) 判断趋势。

策略支持多空双向。只有当随机指标在收盘时向做单方向交叉、MACD 柱状图与信号线出现对应的穿越并且远离零轴、EMA 也给出相同方向的斜率时才允许入场。风险控制与 MQL 版本一致：固定止损、止盈以及基于点差的追踪止损，当价格朝有利方向发展时自动上移或下移保护位。

## 指标

- **MovingAverageConvergenceDivergenceSignal** (`fast = 12`, `slow = 26`, `signal = 9`)：要求 MACD 柱状图在当前柱穿越信号线，并且多头时位于零轴下方、空头时位于零轴上方。`MacdOpenLevel` 与 `MacdCloseLevel` 设定了柱状图距离零轴的最小绝对值。
- **Stochastic** (`Length = 15`, `KPeriod = 3`, `DPeriod = 3`)：%K 对应原脚本中的绿色缓冲区。做多时必须满足 `%K > %D`，做空时相反，同时该条件也负责触发离场。
- **ExponentialMovingAverage** (`Period = 26`)：EMA 作为趋势过滤器，做多需要当前值高于上一根 EMA，做空则需要当前值低于上一根 EMA。

## 入场规则

1. **多头**
   - 收盘时 `%K > %D`。
   - MACD 柱状图 < 0 且高于信号线。
   - 前一根柱子中 MACD < 信号线（当前柱完成向上交叉）。
   - `|MACD| > MacdOpenLevel * 价格步长`。
   - EMA 上升（当前 EMA 大于前一根）。
2. **空头**
   - 收盘时 `%K < %D`。
   - MACD 柱状图 > 0 且低于信号线。
   - 前一根柱子中 MACD > 信号线（当前柱完成向下交叉）。
   - `MACD > MacdOpenLevel * 价格步长`。
   - EMA 下降（当前 EMA 小于前一根）。

若已有持仓，则不会开立新仓，直至原仓位平仓。

## 离场规则

持仓期间持续检查以下条件：

- **指标离场**
  - 多头：`%K < %D`，MACD > 0 且 < 信号线，上一根柱子中 MACD 高于信号线，并且 `|MACD| > MacdCloseLevel * 价格步长`。
  - 空头：`%K > %D`，MACD < 0 且 > 信号线，上一根柱子中 MACD 低于信号线，并且 `|MACD| > MacdCloseLevel * 价格步长`。
- **止损**：`StopLossPoints` * `PriceStep`。
- **止盈**：`TakeProfitPoints` * `PriceStep`。
- **追踪止损**：当浮盈超过 `TrailingStopPoints * PriceStep` 时，将止损上移/下移，确保至少锁定该收益。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 下单手数 | `0.1` |
| `TakeProfitPoints` | 止盈距离（点） | `10` |
| `StopLossPoints` | 止损距离（点） | `50` |
| `TrailingStopPoints` | 追踪止损距离（点） | `5` |
| `MacdOpenLevel` | 入场所需的 MACD 最小绝对值 | `3` |
| `MacdCloseLevel` | 离场所需的 MACD 最小绝对值 | `2` |
| `MacdFastPeriod` | MACD 快速 EMA 长度 | `12` |
| `MacdSlowPeriod` | MACD 慢速 EMA 长度 | `26` |
| `MacdSignalPeriod` | MACD 信号线 EMA 长度 | `9` |
| `EmaPeriod` | 趋势 EMA 的周期 | `26` |
| `StochasticLength` | 随机指标基础周期 | `15` |
| `StochasticKPeriod` | %K 平滑周期 | `3` |
| `StochasticDPeriod` | %D 平滑周期 | `3` |
| `CandleType` | 计算所用的 K 线类型 | `15m` |

## 说明

- 策略仅处理已完成的蜡烛，与原始 MQL `start()` 循环一致。
- 一个“点”由交易品种的 `PriceStep` 决定；若没有提供该值，则默认使用 `1`。
- 完全采用 StockSharp 高阶 API：通过 `SubscribeCandles().BindEx(...)` 绑定指标，不手动维护历史缓冲区，交易使用 `BuyMarket`/`SellMarket` 市价指令。
