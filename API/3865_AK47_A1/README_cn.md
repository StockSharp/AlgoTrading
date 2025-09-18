# AK47 A1
[English](README.md) | [Русский](README_ru.md)

移植自 MetaTrader 专家顾问“AK47_A1”。策略通过比尔·威廉姆斯的鳄鱼指标、DeMarker 振荡器、威廉指标以及分形信号，在市场脱离盘整时才参与交易。

## 细节
- **数据**：使用 `CandleType` 指定的价格蜡烛。
- **指标**：
  - 鳄鱼的下颚/牙齿/嘴唇分别为 13/8/5 周期的平滑均线，向前平移 8/5/3 个柱子，并使用中价计算。
  - DeMarker（周期 13）在多头信号时需 ≥ 0.5，空头信号时需 ≤ 0.5。
  - Williams %R（周期 14）被归一化到 `[0,1]`，上一根柱子必须处于 0.25 与 0.75 之间以避免过度买入或卖出。
  - 分形从最近 5 个高低点中识别，信号在 3 根柱子内有效。
- **入场条件**：
  - 鳄鱼三条线之间的距离都至少达到 `SpanGatorPoints` 点数（无论方向）。
  - **做多**：最新的下方分形仍有效，DeMarker ≥ 0.5，并且 Williams %R 过滤器允许交易。
  - **做空**：最新的上方分形仍有效，DeMarker ≤ 0.5，并且 Williams %R 过滤器允许交易。
  - 开新仓前会平掉相反方向的持仓。
- **出场条件**：
  - 根据 `StopLossPoints` 与 `TakeProfitPoints`（通过合约最小步长转换成价格）设置的固定止损与止盈。
  - 可选的 `TrailingStopPoints` 点跟踪止损，当行情向有利方向发展时沿收盘价移动。
  - 出现反向信号时，先平掉当前仓位再开新仓。
- **默认参数**：
  - `SpanGatorPoints` = 0.5
  - `TakeProfitPoints` = 100
  - `StopLossPoints` = 0（关闭）
  - `TrailingStopPoints` = 50
  - `CandleType` = 1 小时蜡烛
