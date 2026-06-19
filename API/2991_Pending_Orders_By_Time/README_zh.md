# Pending Orders By Time 2 策略

## 概述
该策略复刻了 MetaTrader 中的“Pending orders by time 2”专家顾问：在预先设定的开仓时刻同时挂出突破方向的 Buy Stop 和 Sell Stop。每个挂单都按照交易品种的最小报价步长计算止损与止盈，成交后再配合追踪止损与成对的保护性订单管理仓位。实现完全基于 StockSharp 的高级 API，遵循项目的编码规范（制表符缩进、参数通过 `Param()` 声明等）。

## 日内执行流程
1. **每日初始化**：检测到新交易日的第一根完成 K 线时，重置内部状态，以便在稍后重新挂出当日的突破订单。
2. **开盘挂单**：当 K 线的小时数等于 `OpeningHour`，且当天尚未挂单时，根据最新的最优买价/卖价（若尚无行情则退回到该 K 线的收盘价）计算突破价格，随后同时提交 Buy Stop 与 Sell Stop。
3. **盘中管理**：在交易时段内，策略会为已有仓位动态更新保护性止损，不主动撤销对向的挂单，从而保留向另一侧突破反向开仓的可能。同时等待追踪止损、固定止盈或对向突破触发，从而退出仓位。
4. **收盘处理**：当 K 线的小时数达到 `ClosingHour` 时，立即撤销所有未成交的挂单，并按持仓绝对量发送市价单平仓，避免隔夜持仓。

## 挂单价格计算
- **距离参数**：`DistanceTicks`、`StopLossTicks`、`TakeProfitTicks` 都按报价步长 (`Security.PriceStep`) 解释。Buy Stop 的价格为 `bestAsk + DistanceTicks * step`，止损为该价位下方 `StopLossTicks` 个步长，止盈为同样距离的上方。Sell Stop 的计算完全对称。
- **行情来源**：策略订阅委托簿，持续记录最新的最优买卖价。如果订单簿尚未返回数据，则使用当前完成 K 线的收盘价作为安全的后备值，避免出现零价。
- **订单引用**：保存所有挂出的订单引用，便于在新的一天或收盘时准确地取消、重建订单。

## 仓位与风控管理
- **保护性订单**：当挂单成交（在 `OnOwnTradeReceived` 中捕获）后，立刻按成交量注册一对保护性订单：多头对应 `SellStop` + `SellLimit`，空头对应 `BuyStop` + `BuyLimit`。任何时候仅保持一对保护性订单，新的订单会自动撤销旧的那对。
- **追踪止损**：`TrailingStopTicks` 决定止损与价格的固定距离，`TrailingStepTicks` 指定再次调整所需的最小额外盈利。只有当浮盈大于 `TrailingStop + TrailingStep` 时才会重新计算更优的止损价，取消旧的保护性止损并提交新的订单，且不会放宽已有止损。
- **收盘离场**：到达 `ClosingHour` 时，撤销所有保护性订单，并根据仓位方向发送反向市价单，将持仓恢复为零。

## 可配置参数
- `OpeningHour`：挂出突破挂单的小时（0–23）。
- `ClosingHour`：撤单并强制平仓的小时（0–23）。
- `DistanceTicks`：距离当前最优买卖价的突破幅度，单位为报价步长。
- `StopLossTicks`：初始止损距离，单位为报价步长。
- `TakeProfitTicks`：初始止盈距离，单位为报价步长。
- `TrailingStopTicks`：追踪止损维持的固定距离。
- `TrailingStepTicks`：触发一次新的止损调整所需的最小额外盈利。
- `Volume`：两侧挂单的下单数量。
- `CandleType`：用于控制交易时段的 K 线类型与周期（默认 15 分钟）。

## 实现说明
- 仅使用 StockSharp 的高级策略接口（`SubscribeCandles`、`SubscribeOrderBook`、高阶下单方法等），无需编写低阶指标或缓存集合。
- `OnOwnTradeReceived` 负责在挂单成交时同步保护性订单，并在止损或止盈成交后清理引用。
- 追踪止损逻辑只依赖当前 K 线数据和内部状态，不调用任何 `GetValue` 类接口，符合转换规范。
- 所有距离均通过报价步长计算，等价于 MQL 中基于“点”的做法，适用于不同精度的金融工具。
- 根据任务要求，本目录仅提供 C# 实现；暂未创建 Python 版本。
