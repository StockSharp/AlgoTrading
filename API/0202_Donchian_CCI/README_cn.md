# Donchian CCI Strategy

此策略利用唐奇安通道与CCI指标产生信号。当价格突破上轨且CCI < -100时做多；当价格跌破下轨且CCI > 100时做空，分别代表超卖和超买条件下的突破。

适合在震荡市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Price > Donchian Upper && CCI < -100`
  - 空头: `Price < Donchian Lower && CCI > 100`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破中轨时平仓
  - 空头: 价格升破中轨时平仓
- **止损**: 是
- **默认值**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Donchian CCI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
