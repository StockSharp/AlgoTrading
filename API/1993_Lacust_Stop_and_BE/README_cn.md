# Lacust Stop and BE

该策略演示了一个基础的仓位管理方案，灵感来自原始的 MQL 智能交易系统 **lacuststopandbe**。

策略在根据最后一根完成的 K 线方向开仓后，会应用以下保护规则：

- 初始止损和止盈按固定价差设置。
- 当浮盈达到 `BreakevenGain` 时，止损移动到开仓价并锁定 `Breakeven` 点的利润。
- 当浮盈超过 `TrailingStart` 时，止损按 `TrailingStop` 的距离跟随价格移动。
- 价格触及止损或止盈水平时平仓。

参数：

- `CandleType` – 用于计算的 K 线类型。
- `StopLoss` – 初始止损距离。
- `TakeProfit` – 初始止盈距离。
- `TrailingStart` – 启动跟踪止损所需的利润。
- `TrailingStop` – 跟踪止损与当前价格的距离。
- `BreakevenGain` – 移动止损到保本前所需的利润。
- `Breakeven` – 移动到保本后锁定的利润。

该示例使用 StockSharp 的高级 API，可作为移植简单 MQL 交易管理脚本的模板。
