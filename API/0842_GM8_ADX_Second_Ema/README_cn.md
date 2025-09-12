# GM-8 和 ADX 第二条 EMA 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格与 GM-8 SMA 交叉并符合第二条 EMA，且 ADX 显示强趋势时入场。

## 细节

- **入场条件**：
  - **多头**：价格向上穿越 SMA，且收盘价高于 SMA 和第二条 EMA，并且 ADX 高于阈值。
  - **空头**：价格向下穿越 SMA，且收盘价低于 SMA 和第二条 EMA，并且 ADX 高于阈值。
- **多/空**：双向。
- **出场条件**：
  - **多头**：价格向下穿越 SMA。
  - **空头**：价格向上穿越 SMA。
- **止损**：使用 StartProtection。
- **默认值**：
  - `GM Period` = 15
  - `Second EMA Period` = 59
  - `ADX Period` = 8
  - `ADX Threshold` = 34
  - `Candle Type` = 15m
- **过滤条件**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA、EMA、ADX
  - 止损：是
  - 复杂度：低
  - 时间框架：短期

