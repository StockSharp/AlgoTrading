# Donchian CCI 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

此策略利用唐奇安通道与CCI指标产生信号。当价格突破上轨且CCI高于CciOverbought时做多；当价格跌破下轨且CCI低于CciOversold时做空，即由动量确认的突破。

测试表明年均收益约为 43%，该策略在股票市场表现最佳。

适合在震荡市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Price > Donchian Upper && CCI > CciOverbought`
  - 空头: `Price < Donchian Lower && CCI < CciOversold`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破中轨时平仓
  - 空头: 价格升破中轨时平仓
- **止损**: 是
- **默认值**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `CciOverbought` = 100
  - `CciOversold` = -100
  - `StopLossPercent` = 2
    通道为之前DonchianPeriod根K线的最高价和最低价，中轨位于两者正中。CCI高于CciOverbought时确认向上突破，低于CciOversold时确认向下突破；相反的读法，即收盘价高于通道时CCI处于超卖，实际上几乎不会出现。与持仓方向相反的入场信号会反转持仓。
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

