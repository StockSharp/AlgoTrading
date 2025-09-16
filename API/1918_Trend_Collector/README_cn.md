# 趋势收集策略

该策略是原始 MQL `TrendCollector.mq4` 算法的转换版。它通过两条指数移动平均线结合动量与波动性过滤器来识别趋势。

## 策略逻辑

- **快 EMA 与 慢 EMA**：比较快 EMA 与慢 EMA 来确定主要趋势。
- **随机指标**：判断超买和超卖。当随机值低于下限且快 EMA 高于慢 EMA 时做多；当随机值高于上限且快 EMA 低于慢 EMA 时做空。
- **ATR 波动性过滤器**：只有当 ATR 值低于设定的限制时才进行交易，以避免高波动时期。
- **交易时间窗口**：仅在设定的起始和结束小时之间生成信号。

## 参数

| 名称 | 描述 | 默认值 |
| --- | --- | --- |
| FastMaLength | 快速 EMA 周期 | 4 |
| SlowMaLength | 慢速 EMA 周期 | 204 |
| StochasticPeriod | 随机指标周期 | 14 |
| StochasticUpper | 随机指标上限 | 80 |
| StochasticLower | 随机指标下限 | 20 |
| AtrPeriod | ATR 周期 | 14 |
| AtrLimit | 允许的最大 ATR 值 | 2 |
| StartHour | 交易开始小时 | 5 |
| EndHour | 交易结束小时 | 24 |
| CandleTimeFrame | 蜡烛图时间框架 | 5 分钟 |

当前未提供 Python 版本。
