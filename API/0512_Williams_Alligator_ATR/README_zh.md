# Williams Alligator ATR 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 Williams Alligator 指标并结合 ATR 止损。当 Lips 线向上穿越 Jaw 线时开多单。当 Lips 向下穿越 Jaw 或价格跌至 ATR 止损位时平仓。

## 详情
- **入场**：Lips 向上穿越 Jaw。
- **出场**：Lips 向下穿越 Jaw 或触发 ATR 止损。
- **指标**：Smoothed Moving Average、Average True Range。
- **类型**：仅做多。
