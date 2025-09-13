# MACD Sample 策略
[English](README.md) | [Русский](README_ru.md)

该策略复刻了 MetaTrader 的经典 MACD Sample EA。
它使用 MACD 线与信号线的交叉并结合 EMA 趋势过滤，分别设定多头和空头的止盈止损，同时可选用追踪止损。交易仅在设定的时间窗口内进行。

## 细节

- **入场条件**：
  - **做多**：MACD 线在零线下方并向上穿越信号线，同时 EMA 上升。
  - **做空**：MACD 线在零线上方并向下穿越信号线，同时 EMA 下降。
- **出场条件**：
  - 反向的 MACD 交叉。
  - 达到各自的止盈或止损。
  - 触发追踪止损。
- **多空方向**：均可。
- **默认参数**：
  - `EMA Period` = 26
  - `MACD Open Level` = 3
  - `MACD Close Level` = 2
  - `Take Profit Long` = 50
  - `Take Profit Short` = 75
  - `Stop Loss Long` = 80
  - `Stop Loss Short` = 50
  - `Trailing Stop` = 30
  - 交易时间：UTC 4 点至 19 点
- **指标**：MACD, EMA
- **时间框架**：默认 1 小时 K 线

