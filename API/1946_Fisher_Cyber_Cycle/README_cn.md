# Fisher Cyber Cycle 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 Fisher 变换应用于 Ehlers 的 Cyber Cycle 指标。当 Fisher 线上穿其触发线时开多仓，当下穿触发线时开空仓。出现相反交叉时平仓或反向开仓。

## 详情

- **入场条件**:
  - **多头**: `Fisher > Trigger` 且 `前一 Fisher <= 前一 Trigger`
  - **空头**: `Fisher < Trigger` 且 `前一 Fisher >= 前一 Trigger`
- **出场条件**:
  - Fisher 与 Trigger 的反向交叉
- **止损**: 无
- **默认参数**:
  - `Alpha` = 0.07
  - `Length` = 8
  - `Candle Type` = 8 小时周期
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 多空皆可
  - 指标: Fisher Transform, Cyber Cycle
  - 止损: 无
  - 复杂度: 中等
  - 周期: 中期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险水平: 中等
