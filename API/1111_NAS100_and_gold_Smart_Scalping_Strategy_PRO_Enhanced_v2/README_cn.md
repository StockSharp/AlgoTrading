# NAS100 和黄金 Smart Scalping Strategy PRO Enhanced v2
[English](README.md) | [Русский](README_ru.md)

该策略利用 EMA9 和 VWAP 作为动态参考，RSI 衡量动量，ATR 控制风险。15 分钟 EMA200 趋势过滤器让交易顺势而为，并结合成交量放大过滤强势K线。策略按风险计算仓位，可选跟踪止损并在交易之间设置冷却时间。

## 细节

- **入场条件**: 指标信号
- **多空方向**: 双向
- **出场条件**: 止损、止盈或反向信号
- **止损**: 是，基于 ATR
- **默认值**:
  - `CandleType` = 1 分钟
  - `RiskPercent` = 1%
  - `AtrMultiplierSl` = 1
  - `AtrMultiplierTp` = 2
  - `CooldownMins` = 30
  - `StartHour` = 13
  - `EndHour` = 20
- **过滤器**:
  - 类别: 高频/剥头皮
  - 方向: 双向
  - 指标: EMA, VWAP, RSI, ATR, EMA200
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
