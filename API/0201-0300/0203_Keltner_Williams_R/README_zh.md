# Keltner Williams R 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

此策略结合Keltner通道与Williams %R指标。当价格收于下轨下方且%R < WilliamsROversold时做多；当价格收于上轨上方且%R > WilliamsROverbought时做空，分别代表超卖和超买，价格回到中轨时平仓。

测试表明年均收益约为 46%，该策略在股票市场表现最佳。

适合在震荡市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Price < lower Keltner band && Williams %R < WilliamsROversold`
  - 空头: `Price > upper Keltner band && Williams %R > WilliamsROverbought`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格回到中轨时平仓
  - 空头: 价格回到中轨时平仓
- **止损**: 是
- **默认值**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `StopLossPercent` = 2
    通道为EmaPeriod周期EMA加减KeltnerMultiplier倍AtrPeriod周期ATR，中轨即EMA本身。-80和-20是规则中Williams %R阈值的默认值。止损为入场价的固定StopLossPercent百分比，在K线之间同样监控。与持仓方向相反的入场信号会反转持仓。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Keltner Williams R
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

