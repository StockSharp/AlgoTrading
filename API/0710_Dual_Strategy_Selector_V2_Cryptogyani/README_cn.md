# Dual Strategy Selector V2 - Cryptogyani 策略
[English](README.md) | [Русский](README_ru.md)

该策略在两个仅做多的 SMA 方案之间切换。

- **策略1**：快速 SMA 上穿慢速 SMA，使用固定止盈或跟踪止盈。
- **策略2**：快速 SMA 上穿慢速 SMA，且价格高于高周期 SMA；使用 ATR 止损并部分止盈。

## 详情

- **入场条件**：
  - 策略1：快速 SMA 上穿慢速 SMA。
  - 策略2：快速 SMA 上穿慢速 SMA 且价格高于高周期 SMA。
- **出场条件**：
  - 策略1：达到止盈目标或触发跟踪止损。
  - 策略2：部分止盈后 ATR 止损。
- **指标**：SMA，ATR。
- **方向**：仅做多。
- **止损**：有。
