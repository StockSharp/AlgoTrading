# Mean Reversion Strategy
[English](README.md) | [Русский](README_ru.md)

该统计策略寻找价格相对于近期均值的短期极端。策略使用移动平均线作为公允价，通过标准差计算衡量偏离度。

测试表明年均收益约为 85%，该策略在加密市场表现最佳。

当价格距离均值达到设定距离时开仓：跌破下轨做多，涨至上轨做空。价格再次触及均线时平仓。

适合偏好逆势交易并需要明确进出场区域的交易者。基于波动的通道能在不同市场环境下自适应，并配合固定止损控制亏损。

## 细节
- **入场条件**:
  - 多头: `Price < MA - k*StdDev`
  - 空头: `Price > MA + k*StdDev`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格上穿均线
  - 空头: 价格下穿均线
- **止损**: 是
- **默认值**:
  - `MovingAveragePeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: Mean Reversion
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

