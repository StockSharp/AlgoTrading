# Bober XM 策略
[English](README.md) | [Русский](README_ru.md)

Bober XM 策略基于自定义的 Keltner 通道。 当价格突破通道并在加权移动平均线和 ADX 强度确认下进入，OBV 与其均线交叉并且 ADX 仍然强劲时退出。

该方法适合需要动量确认和基于成交量退出的交易者。

## 细节

- **入场条件**:
  - **做多**: `Close > UpperBand && Close > WMA && ADX > Threshold`
  - **做空**: `Close < LowerBand && Close < WMA && ADX > Threshold`
- **方向**: 双向
- **出场条件**:
  - **多头**: `OBV < OBV_MA && ADX > Threshold`
  - **空头**: `OBV > OBV_MA && ADX > Threshold`
- **止损**: 使用 `StopLossPercent` 百分比止损
- **默认值**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `KeltnerMultiplier` = 1.5m
  - `WmaPeriod` = 15
  - `ObvMaPeriod` = 22
  - `AdxPeriod` = 60
  - `AdxThreshold` = 35m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: Keltner、WMA、OBV、ADX
  - 止损: 是
  - 复杂度: 中等
  - 周期: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险水平: 中等

