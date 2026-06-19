# Cronex AC

Cronex AC 策略在 StockSharp 高阶 API 中还原了经典的 Cronex Acceleration/Deceleration (AC) 专家顾问。策略将 Accelerator 振荡器连续进行两次平滑处理，当快速线与信号线发生交叉时执行开仓或平仓：向上交叉开多单、平空单，向下交叉开空单、平多单。

## 交易逻辑

1. 根据选定的 K 线序列计算 Accelerator/Deceleration 振荡器数值。
2. 按照所选类型对振荡器进行两次移动平均平滑：第一次得到“快线”，第二次得到“信号线”。
3. 在 `SignalBar` 指定的历史 K 线上读取信号，并额外向前查看一根 K 线以确认是否发生真正的穿越。
4. 当快线上穿信号线时（若允许），策略先平掉空头仓位，再开立多头仓位。
5. 当快线下穿信号线时（若允许），策略先平掉多头仓位，再开立空头仓位。
6. 下单手数等于参数 `Volume` 加上当前持仓的绝对值，使策略能够通过一次市价单直接完成反手。

策略完全基于收盘后的数据做决策，并保留了 MQL5 版本中针对多、空方向分别授权的开仓与平仓开关。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `SmoothingType` | `CronexMovingAverageType` | `Simple` | 应用于 AC 振荡器的移动平均类型，可选：Simple、Exponential、Smoothed、Weighted。 |
| `FastPeriod` | `int` | `14` | 第一次平滑的周期（快线）。 |
| `SlowPeriod` | `int` | `25` | 第二次平滑的周期（信号线）。 |
| `SignalBar` | `int` | `1` | 读取信号所使用的已完成 K 线偏移，1 代表上一根 K 线，与原版 Cronex 保持一致。 |
| `CandleType` | `DataType` | `TimeFrame(8h)` | 计算所使用的 K 线类型。 |
| `EnableLongEntry` | `bool` | `true` | 允许在向上交叉时开多。 |
| `EnableShortEntry` | `bool` | `true` | 允许在向下交叉时开空。 |
| `EnableLongExit` | `bool` | `true` | 允许在快线跌破信号线时平多。 |
| `EnableShortExit` | `bool` | `true` | 允许在快线上破信号线时平空。 |
| `Volume` | `decimal` | 策略默认 | 下单数量。策略会自动加上当前持仓的绝对值，以便一次性完成反手。 |

## 图表展示

当界面存在图表区域时，策略会绘制：

- 选定周期的价格 K 线；
- Accelerator 振荡器曲线；
- 快线与信号线；
- 策略自身的成交记录，用于直观验证。

## 说明

- 所有计算都基于已完成的 K 线 (`CandleStates.Finished`)，避免信号重绘。
- 内部缓冲区仅保存满足 `SignalBar` 位移所需的最少数据，保持与 MQL 原策略一致的行为。
- 原始 MQL 版本中的止损、止盈及滑点参数未移植，可在 StockSharp 的风险管理模块中单独配置。
