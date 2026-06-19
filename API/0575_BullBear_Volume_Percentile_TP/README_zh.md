# BullBear Volume Percentile TP 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 Bull/Bear Power 指标并通过 Z-Score 标准化。
当 Z-Score 向上突破阈值时做多，向下跌破负阈值时做空。
止盈价格基于 ATR 倍数，并根据成交量与价格百分位进行调整。

## 细节

- **入场条件：**
  - **多头**：Z-Score 上穿 `ZThreshold`。
  - **空头**：Z-Score 下穿 `-ZThreshold`。
- **多空方向**：双向。
- **出场条件**：Z-Score 回到零附近或触发止盈。
- **止损/止盈**：ATR 倍数止盈。
- **默认参数：**
  - EMA 长度 21，Z-Score 长度 252，阈值 1.618。
  - ATR 周期 20，倍数 1.618 / 2.382 / 3.618。
  - 成交量均线周期 100，百分位周期 100。
- **筛选：**
  - 类型：动量
  - 方向：双向
  - 指标：EMA、ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：中期
