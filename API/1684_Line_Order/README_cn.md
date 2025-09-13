# 线条订单策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 智能交易系统 "MyLineOrder" 的 StockSharp 版本。交易者可以设置水平价格线，当价格触及这些水平时自动执行市价单。止损、止盈和跟踪止损以点数表示，交易量也可配置。

当市场价格触及 **BuyPrice** 时策略做多；触及 **SellPrice** 时策略做空。开仓后策略根据止损、止盈或跟踪止损条件监控并退出仓位。

## 细节

- **入场条件**：
  - **多头**：价格触及或突破 `BuyPrice`。
  - **空头**：价格触及或跌破 `SellPrice`。
- **方向**：双向。
- **出场条件**：
  - 止损、止盈或跟踪止损。
- **止损**：
  - `StopLossPips`、`TakeProfitPips`、`TrailingStopPips`。
- **过滤器**：
  - 无。
- **参数**：
  - `BuyPrice` – 做多入场水平。
  - `SellPrice` – 做空入场水平。
  - `StopLossPips` – 止损点数。
  - `TakeProfitPips` – 止盈点数。
  - `TrailingStopPips` – 跟踪止损点数。
  - `TradeVolume` – 交易量。
  - `CandleType` – 用于监控价格的蜡烛时间框。
