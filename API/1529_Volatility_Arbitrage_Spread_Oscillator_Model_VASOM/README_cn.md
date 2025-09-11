# 波动率套利价差振荡模型 (VASOM)
[English](README.md) | [Русский](README_ru.md)

当前月 VIX 期货与次月合约之间的价差 RSI 低于阈值时做多前月合约, 当 RSI 上穿退出阈值时平仓。

## 详情
- **入场条件**: 价差 RSI < `LongThreshold`.
- **多空方向**: 仅做多。
- **出场条件**: 价差 RSI > `ExitThreshold`.
- **止损**: 无。
- **默认值**:
  - `RsiPeriod` = 2
  - `LongThreshold` = 46
  - `ExitThreshold` = 76
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SecondSecurity` = "CBOE:VX2!"
- **过滤器**:
  - 类别: Volatility
  - 方向: Long
  - 指标: RSI
  - 止损: No
  - 复杂度: Beginner
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
