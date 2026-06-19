# Altarius RSI 随机指标策略

## 概述

Altarius RSI 随机指标策略是 MetaTrader 专家顾问 `AltariusRSIxampnSTOH` 的移植版本。策略将两个不同周期的随机指标与一个短周期 RSI 结合：慢速随机指标负责确认趋势方向以及超买/超卖区域，快速随机指标用于衡量动量强度。平仓由 RSI 与慢速随机指标的信号线共同决定。同时保留了原始 MQL 脚本中的资金管理特性，例如在连续亏损后缩减手数以及基于权益的回撤保护。

## 交易逻辑

1. **数据源**：使用可配置的时间周期蜡烛图（默认 15 分钟），所有指标均基于收盘价计算。
2. **入场条件**
   - **做多**：慢速随机指标主线 (15,8,8) 位于信号线之上但仍低于 `BuyStochasticLimit`（默认 50）。快速随机指标 (10,3,3) 的主线与信号线之差绝对值大于 `StochasticDifferenceThreshold`（默认 5），表明动量足够强。
   - **做空**：慢速随机指标主线位于信号线之下但仍高于 `SellStochasticLimit`（默认 55）。快速随机指标同样需要满足动量阈值。
3. **离场条件**
   - **多头离场**：当 RSI(周期 4) 大于 `ExitRsiHigh`（60），且慢速随机指标信号线较上一根蜡烛下降并且仍高于 `ExitStochasticHigh`（70）。
   - **空头离场**：当 RSI 低于 `ExitRsiLow`（40），且慢速随机指标信号线上升至上一根蜡烛之上并且仍低于 `ExitStochasticLow`（30）。
   - **风险控制离场**：若浮动盈亏跌破允许的权益回撤比例 (`MaximumRiskPercent`)，立即关闭所有仓位。
4. **仓位管理**：以 `BaseVolume` 作为起始手数，在连续亏损时根据 `DecreaseFactor` 缩减下笔交易的手数；同时会依据品种最小/最大成交量及步长对下单量进行规范。

## 参数说明

| 参数 | 说明 |
|------|------|
| `BaseVolume` | 基础下单手数，风险管理前的初始值。 |
| `MaximumRiskPercent` | 当浮亏占账户权益比例超过该数值时强制平仓。 |
| `DecreaseFactor` | 连续亏损时缩减手数所用的除数。 |
| `RsiPeriod` | 用于离场判断的 RSI 周期。 |
| `SlowStochasticPeriod`, `SlowStochasticK`, `SlowStochasticD` | 慢速随机指标的配置。 |
| `FastStochasticPeriod`, `FastStochasticK`, `FastStochasticD` | 快速随机指标的配置。 |
| `StochasticDifferenceThreshold` | 快速随机指标主线与信号线之间的最小差值，用于确认动量。 |
| `BuyStochasticLimit`, `SellStochasticLimit` | 慢速随机指标入场时允许的区间上/下限。 |
| `ExitRsiHigh`, `ExitRsiLow` | 触发多头或空头离场的 RSI 阈值。 |
| `ExitStochasticHigh`, `ExitStochasticLow` | 确认离场的慢速随机指标信号线阈值。 |
| `CandleType` | 指标计算所使用的蜡烛类型。 |

## 备注

- 策略一次只持有一个方向的仓位，与原始 EA 行为一致。
- 连续亏损及回撤保护使用 StockSharp 组合的实时权益信息进行计算。
- 若创建了图表区域，策略会绘制蜡烛、两个随机指标以及成交标记，方便可视化分析。
