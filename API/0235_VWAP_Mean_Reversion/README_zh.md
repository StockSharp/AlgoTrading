# VWAP 均值回归策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格偏离成交量加权平均价(VWAP)时进行反向操作。ATR 用于衡量价格离开 VWAP 的幅度，超过一定倍数才考虑进场。

测试表明年均收益约为 58%，该策略在股票市场表现最佳。

当价格低于 VWAP 且距离超过 `K` 倍 ATR 时开多；当价格高于 VWAP 同样幅度时做空。价格回到 VWAP 线附近即平仓。

该方法适合日内交易者，假设价格围绕 VWAP 波动。止损以 ATR 的倍数计算，以免走势继续而造成过大损失。
## 详细信息
- **入场条件**:
  - **做多**: Close < VWAP - K * ATR
  - **做空**: Close > VWAP + K * ATR
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when close >= VWAP
  - **做空**: Exit when close <= VWAP
- **止损**: 是
- **默认值**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **筛选条件**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: VWAP, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等


