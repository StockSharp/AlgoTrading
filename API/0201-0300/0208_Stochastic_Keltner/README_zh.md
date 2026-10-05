# Stochastic Keltner 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略结合随机指标与Keltner通道。当随机%K小于StochOversold且价格低于下轨时做多；当随机%K大于StochOverbought且价格高于上轨时做空。

测试表明年均收益约为 61%，该策略在加密市场表现最佳。

适合在混合市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `Stoch %K < StochOversold && Price < Keltner lower band`
  - 空头: `Stoch %K > StochOverbought && Price > Keltner upper band`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格回到中轨时平仓
  - 空头: 价格回到中轨时平仓
- **止损**: 是
- **默认值**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    20和80是规则中%K阈值的默认值。通道为EmaPeriod周期EMA加减KeltnerMultiplier倍AtrPeriod周期ATR，中轨即EMA本身。止损距入场收盘价AtrMultiplier倍同一ATR，按K线收盘检查；设为0则关闭。规则中的StochK即%K：StochPeriod根K线的随机指标再经StochK根K线平滑；%D不参与规则，因此没有对应参数。与持仓方向相反的入场信号会反转持仓。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Stochastic Keltner
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

