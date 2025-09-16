# DoubleUp2 CCI MACD 策略
[English](README.md) | [Русский](README_ru.md)

DoubleUp2 是一种结合商品通道指数 (CCI) 和 MACD 的马丁格尔策略。
当两个指标都处于极端正值时开空；当两个指标都处于极端负值时开多。
若上一次交易亏损，仓位规模会加倍以试图弥补损失。
当价格向盈利方向移动固定点数时，策略将平仓。

## 细节

- **入场条件**：
  - **多头**：`CCI < -Threshold` 且 `MACD < -Threshold`。
  - **空头**：`CCI > Threshold` 且 `MACD > Threshold`。
- **多/空**：双向。
- **出场条件**：
  - 出现反向信号或价格向盈利方向移动 `ExitDistance` 点。
- **止损**：无显式止损。
- **默认值**：
  - `CCI Period` = 8
  - `MACD Fast` = 13
  - `MACD Slow` = 33
  - `MACD Signal` = 2
  - `Threshold` = 230
  - `Base Volume` = 0.1
  - `ExitDistance` = `120 * price step`
- **过滤器**：
  - 分类：反转
  - 方向：双向
  - 指标：CCI、MACD
  - 止损：无
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：高
