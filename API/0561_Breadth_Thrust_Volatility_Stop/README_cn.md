# Breadth Thrust Strategy with Volatility Stop-Loss
[English](README.md) | [Русский](README_ru.md)

该策略在市场广度强劲上升时进行交易。它利用上涨和下跌股票数量（以及可选的成交量）计算广度比率，并用移动平均进行平滑。当平滑后的值上穿低阈值时开多单。风险控制通过基于ATR的跟踪止损和固定持有期完成。

## 细节
- **入场**：平滑广度比率上穿 `Low Threshold`。
- **离场**：
  - 价格触及 `Volatility Multiplier * ATR` 的跟踪止损；
  - 持仓达到 `Hold Periods` 根K线。
- **参数**：
  - `Smoothing Length` – SMA周期；
  - `Low Threshold` – 广度触发水平；
    - `Use Volume` – 是否加入成交量比率；
  - `Hold Periods` – 持有K线数量；
  - `Volatility Multiplier` – ATR倍数止损；
  - `Candle Type` – 使用的K线类型。
