# Indicator Panel 策略
[English](README.md) | [Русский](README_ru.md)

显示当前品种的 RSI、MACD、DMI、CCI、MFI、动量以及两条移动平均线。策略仅记录指标值，不进行交易。

## 细节

- **入场条件**: 无
- **多空方向**: 无
- **出场条件**: 无
- **止损**: 无
- **默认值**:
  - `RsiLength` = 14
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `DiLength` = 14
  - `AdxLength` = 14
  - `CciLength` = 20
  - `MfiLength` = 20
  - `MomentumLength` = 10
  - `Ma1IsEma` = false
  - `Ma1Length` = 50
  - `Ma2IsEma` = false
  - `Ma2Length` = 200
- **过滤器**:
  - 类别: 信息型
  - 方向: 无
  - 指标: RSI, MACD, DMI, CCI, MFI, Momentum, MA
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 低
