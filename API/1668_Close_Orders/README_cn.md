# Close Orders Strategy
[English](README.md) | [Русский](README_ru.md)

该工具策略用于根据用户设定的过滤条件立即平掉现有仓位并取消挂单。它既可以只处理所选品种，也可以遍历整个投资组合。可选的时间窗口和价格区间限制能精确控制被处理的订单。

## 细节

- **目的**：风险控制和手动平仓。
- **工作流程**：
  - 启动时检查可选的时间窗口。
  - 如果时间允许，按过滤条件关闭仓位并取消挂单。
  - 完成后策略会自动停止。
- **过滤条件**：
  - `CloseAllSecurities` – 处理投资组合中的所有品种，而不仅是当前品种。
  - `CloseOpenLongOrders` / `CloseOpenShortOrders` – 平掉多头或空头仓位。
  - `ClosePendingLongOrders` / `ClosePendingShortOrders` – 取消买入或卖出挂单。
  - `SpecificOrderId` – 非零时只处理指定交易 ID 的订单。
  - `CloseOrdersWithinRange`、`CloseRangeHigh`、`CloseRangeLow` – 按开仓价区间限制。
  - `EnableTimeControl`、`StartCloseTime`、`StopCloseTime` – 仅在特定时间段执行。
- **默认值**：
  - 所有关闭选项均启用。
  - `SpecificOrderId` = 0。
  - `CloseOrdersWithinRange` = false。
  - `CloseRangeHigh` = 0。
  - `CloseRangeLow` = 0。
  - `EnableTimeControl` = false。
  - `StartCloseTime` = 02:00。
  - `StopCloseTime` = 02:30。
- **说明**：
  - 策略不会开立新的仓位。
  - 当价格区间的上下限为零或负数时，忽略该过滤条件。
  - 启用 `CloseAllSecurities` 时，将处理投资组合中的所有品种。
