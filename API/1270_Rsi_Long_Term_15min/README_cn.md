# RSI Long-Term 策略 15min
[English](README.md) | [Русский](README_ru.md)

该策略结合RSI超卖信号、长期移动平均线和成交量确认来开多单。当RSI低于30、SMA(250)高于SMA(500)，且成交量大于其20周期SMA的2.5倍时买入。

## 细节

- **入场条件**: RSI低于30，SMA(250)高于SMA(500)，成交量超过其20周期SMA的2.5倍
- **多空方向**: 仅多头
- **出场条件**: SMA(250)下穿SMA(500)或触发止损
- **止损**: 是，固定百分比
- **默认值**:
  - `RsiLength` = 10
  - `VolumeSmaLength` = 20
  - `Sma1Length` = 250
  - `Sma2Length` = 500
  - `VolumeMultiplier` = 2.5
  - `StopLossPercent` = 5
- **过滤器**:
  - 分类: 趋势
  - 方向: 多头
  - 指标: RSI, SMA
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
