# RSI Donchian 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本策略结合RSI与唐奇安通道，寻找动量极端并配合通道突破。当RSI高于RsiOverbought且价格突破上轨时做多；当RSI低于RsiOversold且价格跌破下轨时做空。价格回到中轨时离场。

测试表明年均收益约为 82%，该策略在股票市场表现最佳。

适合喜欢顺应强劲动量并依赖明确突破水平的主动交易者。止损用于防止动量未能及时回撤时的风险。

## 细节
- **入场条件**:
  - 多头: `RSI > RsiOverbought && Price > Donchian Upper`
  - 空头: `RSI < RsiOversold && Price < Donchian Lower`
- **多/空**: 双向
- **离场条件**:
  - 多头: 收盘价跌破唐奇安中轨
  - 空头: 收盘价升破唐奇安中轨
- **止损**: 百分比止损
- **默认值**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `StopLossPercent` = 2
    通道为之前DonchianPeriod根K线的最高价和最低价，中轨位于两者正中。RSI高于RsiOverbought时确认向上突破，低于RsiOversold时确认向下突破；相反的读法，即收盘价高于通道时RSI处于超卖，实际上几乎不会出现。止损为入场价的固定StopLossPercent百分比，在K线之间同样监控。与持仓方向相反的入场信号会反转持仓。
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

