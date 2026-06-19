# RSI Stochastic WMA 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 RSI、随机振荡指标和加权移动平均线 (WMA)。
当 RSI 低于 30，%K 上穿 %D 且价格高于 WMA 时做多。
当 RSI 高于 70，%K 下穿 %D 且价格低于 WMA 时做空。

## 细节

- **入场条件**:
  - 多头: `RSI < 30 && %K 上穿 %D && Close > WMA`
  - 空头: `RSI > 70 && %K 下穿 %D && Close < WMA`
- **多空方向**: 都支持
- **止损**: 无
- **默认值**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**:
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: RSI, Stochastic, WMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
