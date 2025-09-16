# PZ Parabolic SAR EA
[English](README.md) | [Русский](README_ru.md)

该策略复刻了 *PZ Parabolic SAR* 专家顾问。它使用两个 Parabolic SAR 指标，分别具有不同的步长和最大加速度参数。"交易" SAR 用于判断趋势方向并产生入场信号，而 "止损" SAR 紧跟价格，当趋势反转时触发退出。

风险控制通过平均真实波幅 (ATR) 实现。开仓时根据 ATR 设置初始止损，可选的 ATR 跟踪止损会在行情向有利方向发展时收紧止损。当利润超过初始止损距离时，策略会部分平仓（默认 50%）并将止损移至保本位置。

策略可双向交易，仅处理已完成的K线，并使用市价单执行，无真实止损委托。

## 细节

- **入场条件**：价格与交易SAR同向且止损SAR位于同侧。
- **交易方向**：多空双向。
- **离场条件**：价格穿越止损SAR或触发 ATR 跟踪止损。
- **止损方式**：基于ATR的止损，可选跟踪止损与保本。
- **默认参数**：
  - `TradeStep` = 0.002
  - `TradeMax` = 0.2
  - `StopStep` = 0.004
  - `StopMax` = 0.4
  - `AtrPeriod` = 30
  - `AtrMultiplier` = 2.5
  - `UseTrailing` = false
  - `TrailingAtrPeriod` = 30
  - `TrailingAtrMultiplier` = 1.75
  - `PartialClosing` = true
  - `PercentageToClose` = 0.5
  - `BreakEven` = true
  - `LotSize` = 0.1
  - `CandleType` = TimeFrame(5m)
- **筛选**：
  - 类别：Trend
  - 方向：Both
  - 指标：Parabolic SAR, ATR
  - 止损：ATR, Trailing
  - 复杂度：中等
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等

