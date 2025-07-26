# RSI Donchian Strategy
[English](README.md) | [Русский](README_ru.md)

本策略结合RSI与唐奇安通道，寻找动量极端并配合通道突破。当RSI低于30且价格突破上轨时做多；当RSI高于70且价格跌破下轨时做空。价格回到中轨时离场。

测试表明年均收益约为 82%，该策略在股票市场表现最佳。

适合喜欢在耗尽走势后反向交易但又依赖突破水平的主动交易者。止损用于防止动量未能及时回撤时的风险。

## 细节
- **入场条件**:
  - 多头: `RSI < 30 && Price > Donchian Upper`
  - 空头: `RSI > 70 && Price < Donchian Lower`
- **多/空**: 双向
- **离场条件**:
  - 多头: 收盘价跌破唐奇安中轨
  - 空头: 收盘价升破唐奇安中轨
- **止损**: 百分比止损
- **默认值**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: RSI, Donchian Channel
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

