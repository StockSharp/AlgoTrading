# CoeffofLine 真 实 策略

该策略将 MQL5 专家 `Exp_CoeffofLine_true.mq5` 移植到 StockSharp 框架。它跟踪中位价的**线性回归斜率**，并在斜率穿越零轴时做出反应。

当斜率由负转正时开多头仓位；当斜率由正转负时开空头仓位。出现相反信号时平掉现有仓位。策略仅处理已完成的K线。

## 参数

- **Candle Type** – K线时间框架。
- **Slope Period** – 计算斜率的线性回归长度。
- **Signal Bar** – 用于评估信号的历史K线索引。
- **Buy Open / Sell Open** – 是否允许开多或开空。
- **Buy Close / Sell Close** – 是否允许平多或平空。

策略通过高级 API 订阅K线并绑定指标，无需手动读取指标值。
