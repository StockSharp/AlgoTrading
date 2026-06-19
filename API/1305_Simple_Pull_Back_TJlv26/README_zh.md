# Simple Pull Back TJlv26 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格高于长周期SMA、低于短周期SMA且RSI(3)低于30并位于指定日期范围内时买入。退出条件为按百分比设定的止损和止盈，或当价格高于短期SMA但低于前一根K线最低价时平仓。

## 详情

- **入场条件**：
  - **多头**：收盘价 > 长期SMA，收盘价 < 短期SMA，RSI(3) < 30，时间介于 StartDate 与 EndDate。
- **出场条件**：
  - 止损：价格 ≤ 入场价 × (1 − StopLossPercent/100)。
  - 止盈：价格 ≥ 入场价 × (1 + TakeProfitPercent/100)。
  - 若价格 > 短期SMA 且价格 < 前一根K线最低价则平仓。
- **指标**：SMA、RSI。
- **止损**：有。
- **方向**：仅做多。
