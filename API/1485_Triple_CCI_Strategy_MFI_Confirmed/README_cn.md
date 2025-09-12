# Triple CCI MFI Confirmed 策略
[English](README.md) | [Русский](README_ru.md)

当快速CCI上穿零轴且中慢CCI保持为正、价格高于EMA并且MFI超过50时，该策略做多。激活基于ATR后，使用EMA进行移动止盈。

测试显示该策略有中等表现，在趋势市场中效果更好。

## 细节
- **入场条件**:
  - 多头: 快速CCI上穿0， 中CCI > 0， 慢CCI > 0， MFI > 50， 收盘价高于EMA
- **多空方向**: 仅多头
- **离场条件**:
  - 多头: 激活后收盘价跌破追踪EMA或最低价触及ATR止损
- **止损**: 是
- **默认值**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 14
  - `MiddleCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `MfiLength` = 14
  - `EmaLength` = 50
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Long
  - 指标: CCI, MFI, EMA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
