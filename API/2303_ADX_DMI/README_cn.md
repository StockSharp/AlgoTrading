# ADX DMI 策略
[English](README.md) | [Русский](README_ru.md)

使用方向移动指数（DMI）交易 +DI 与 -DI 线的交叉。当上一根K线的 -DI 高于 +DI 而当前K线跌破 +DI 时，策略开多仓。上一根K线的 +DI 高于 -DI 而当前K线跌破 -DI 时，策略开空仓。反向信号可选择性关闭已有仓位。

## 详情

- **入场条件**：
  - **多头**：上一根 -DI > +DI，当前 -DI 下穿 +DI。
  - **空头**：上一根 +DI > -DI，当前 +DI 下穿 -DI。
- **出场条件**：
  - 反向交叉（若启用相应的平仓选项）。
- **指标**：
  - Directional Index（默认周期 14）
- **止损**：默认无。
- **默认值**：
  - `DmiPeriod` = 14
  - `AllowLong` = true
  - `AllowShort` = true
  - `CloseLong` = true
  - `CloseShort` = true
- **过滤条件**：
  - 适用于任何时间周期
  - 指标：DMI
  - 止损：可通过外部风控模块设置
  - 复杂度：基础
