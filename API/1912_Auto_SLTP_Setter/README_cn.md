# Auto SL-TP Setter Strategy

该工具型策略在持仓没有止损或止盈时自动添加相应订单。距离可以设置为固定点数或ATR倍数。

## 参数

- `Candle Type` – 用于计算ATR的时间框架。
- `Set Stop Loss` – 是否自动设置止损。
- `Set Take Profit` – 是否自动设置止盈。
- `Stop Loss Method` – 1 = 固定点数, 2 = ATR倍数。
- `Fixed SL (pips)` – 当使用固定方法时的止损点数。
- `SL ATR Multiplier` – 当使用ATR方法时的止损ATR倍数。
- `Take Profit Method` – 1 = 固定点数, 2 = ATR倍数。
- `Fixed TP (pips)` – 当使用固定方法时的止盈点数。
- `TP ATR Multiplier` – 当使用ATR方法时的止盈ATR倍数。
- `ATR Period` – ATR的计算周期数。

## 工作原理

1. 启动时策略检查配置。
2. 如果选择基于ATR的方式，则订阅指定的K线并计算ATR。
3. ATR可用后，策略使用计算出的距离调用 `StartProtection`。
4. `StartProtection` 为当前以及之后的持仓挂出止损和止盈单。

该策略不产生交易信号，仅用于风险管理，确保每个持仓都有合适的止损和止盈水平。
