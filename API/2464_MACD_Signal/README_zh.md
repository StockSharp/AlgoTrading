# MACD 信号策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 MACD 线与其信号线的差值进行交易。
当该差值突破基于 ATR 的阈值时开仓，反向突破时平仓。
策略使用以 tick 为单位的移动止损和固定止盈。

## 细节

- **入场条件**：
  - **多头**：MACD - Signal 上穿 `ATR * Level`。
  - **空头**：MACD - Signal 下穿 `-ATR * Level`。
- **方向**：双向。
- **出场条件**：
  - 反向突破阈值。
- **止损/止盈**：
  - 固定 tick 止盈。
  - 可选移动止损。
- **指标**：
  - MACD（可调节 fast、slow、signal 周期）。
  - ATR(200) 用于计算阈值。
