# Hurst Exponent Reversion Strategy

该策略利用赫斯特指数检测市场是否具备均值回归特性。当指数低于0.5时，价格倾向回到平均值，可在极端处反向。

当赫斯特指数低于0.5且收盘价在均线下方时做多；当指数低于0.5且收盘价在均线上方时做空。价格回到均线或指数升至阈值上方时平仓。

适合偏好统计倾向而非强趋势的交易者。百分比止损能在价格未能回归时提供保护。

## 细节
- **入场条件**:
  - 多头: `Hurst < 0.5 && Close < MA`
  - 空头: `Hurst < 0.5 && Close > MA`
- **多/空**: 双向
- **离场条件**:
  - 多头: Close >= MA 或 Hurst > 0.5
  - 空头: Close <= MA 或 Hurst > 0.5
- **止损**: 百分比止损
- **默认值**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: Hurst Exponent, MA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
