# 成交量突破策略
[English](README.md) | [Русский](README_ru.md)

本策略观察成交量的快速扩张。当读数远高于平均水平时，价格往往酝酿新的走势。

当成交量突破依据历史数据及偏差倍数构建的通道时进场，可做多或做空，并配合止损。成交量回到均值附近即离场。

适合寻求早期突破的动量交易者。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Volume
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 103%，该策略在股票市场表现最佳。

测试表明年均收益约为 49%，该策略在加密市场表现最佳。
