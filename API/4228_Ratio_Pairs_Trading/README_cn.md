# 价比配对交易策略
[English](README.md) | [Русский](README_ru.md)

该策略跟踪两只相关资产之间的价格比率 (Asset1 / Asset2)。对该比率应用布林带
指标，得到其滚动均值和标准差通道；再根据当前比率相对均值的偏离度计算 z-score，
从而驱动入场与出场。

当 z-score 超过入场阈值时，比率被视为相对均衡水平过高，策略卖出 Asset1 并买入
Asset2；当 z-score 跌破负阈值时则开出反向配对。只要 z-score 回落到出场区间内，
即比率重新靠近均值，就会平掉整个配对。

`HedgeRatio` 参数用于调整第二条腿的手数，使整组配对接近美元或贝塔中性。每条腿
还启用了百分比止损，以应对两者关系暂时失效的情形。

## 细节
- **入场条件**:
  - 多头配对（多 Asset1 / 空 Asset2）：比率 Z-Score <= -`EntryZScore`
  - 空头配对（空 Asset1 / 多 Asset2）：比率 Z-Score >= `EntryZScore`
- **多/空**: 双向
- **离场条件**:
  - Z-Score 绝对值回落到 `ExitZScore` 以内时平仓
- **止损**: 每条腿均设置百分比止损
- **默认值**:
  - `LookbackPeriod` = 20
  - `EntryZScore` = 2.0m
  - `ExitZScore` = 0.5m
  - `HedgeRatio` = 1.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Arbitrage
  - 方向: 双向
  - 指标: 比率布林带, Z-Score
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等
