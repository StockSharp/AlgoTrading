# Auto Trailing Stop
[English](README.md) | [Русский](README_ru.md)

该策略在已有仓位上自动附加止损和止盈订单，并在价格向有利方向移动时跟踪止损。

## 细节
- **入场条件**：无，策略不会自行开仓。
- **多空方向**：对已有的多头和空头仓位都有效。
- **出场条件**：止损和止盈订单。价格移动超过设定距离的一半后更新跟踪止损。
- **止损**：仓位出现时立即放置初始止损和止盈；止损按照 `TrailingStopStep` 逐步跟踪。
- **默认值**：TrailingStop 6，TrailingStopStep 1，TakeProfit 35，StopLoss 114。
- **过滤器**：可通过参数关闭跟踪止损、自动止盈或自动止损。

## 参数
- `FridayTrade` - 是否允许在周五跟踪。
- `UseTrailingStop` - 启用跟踪止损。
- `AutoTrailingStop` - 为真时使用默认距离 6。
- `TrailingStop` - 当 `AutoTrailingStop` 为假时的跟踪距离。
- `TrailingStopStep` - 跟踪止损移动的最小步长。
- `AutomaticTakeProfit` - 自动放置止盈订单。
- `TakeProfit` - 止盈距离。
- `AutomaticStopLoss` - 自动放置止损订单。
- `StopLoss` - 止损距离。
- `CandleType` - 用于价格更新的K线类型（默认为1分钟）。
