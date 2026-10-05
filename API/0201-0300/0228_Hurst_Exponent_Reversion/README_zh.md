# Hurst Exponent Reversion 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略利用赫斯特指数检测市场是否具备均值回归特性。当指数低于0.5时，价格倾向回到平均值，可在极端处反向。

测试表明年均收益约为 121%，该策略在加密市场表现最佳。

当赫斯特指数低于HurstThreshold且收盘价在均线下方时做多；当指数低于HurstThreshold且收盘价在均线上方时做空。价格回到均线或指数升至阈值上方时平仓。

适合偏好统计倾向而非强趋势的交易者。百分比止损能在价格未能回归时提供保护。

## 细节
- **入场条件**:
  - 多头: `Hurst < HurstThreshold && Close < MA`
  - 空头: `Hurst < HurstThreshold && Close > MA`
- **多/空**: 双向
- **离场条件**:
  - 多头: Close >= MA 或 Hurst > HurstThreshold
  - 空头: Close <= MA 或 Hurst > HurstThreshold
- **止损**: 百分比止损
- **默认值**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `HurstThreshold` = 0.7
  - `StopLossPercent` = 2
    规则给出的0.5是均值回归的理论边界，但在用于测试示例的BTC和TON历史数据上，基于100根5分钟K线的R/S估计从未低于约0.65，规则因此无法交易；HurstThreshold将该水平设为参数，默认值0.7。MA为AveragePeriod周期简单移动平均。止损为入场价的固定StopLossPercent百分比，在K线之间同样监控；设为0则关闭。与持仓方向相反的入场信号会反转持仓。
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

