# Heikin Ashi V2 策略
[English](README.md) | [Русский](README_ru.md)

该版本在通用 Heikin Ashi 策略上加入 EMA 过滤器。仅当 Heikin Ashi 蜡烛方向与 EMA 定义的趋势一致时才进场，从而避免单纯 HA 可能产生的逆势信号。

## 详情

- **入场条件**:
  - **多头**: `HA_Close > HA_Open` 且 `Close > EMA`
  - **空头**: `HA_Close < HA_Open` 且 `Close < EMA`
- **多空方向**: 双向
- **退出条件**:
  - 反向信号
- **止损**: 无
- **默认值**:
  - `EmaLength` = 20
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: Heikin Ashi, EMA
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
