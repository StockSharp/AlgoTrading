# Informative Dashboard 策略

Informative Dashboard 策略是 MetaTrader 5 专家顾问 “Informative Dashboard EA” 的 StockSharp 高阶 API 移植版本。原始脚本构建了一个仪表盘面板，持续刷新账号名称、当日已实现盈亏、当前回撤、未平仓头寸与挂单数量以及实时点差。StockSharp 版本保持为信息面板，不包含任何自动交易逻辑——所有数据都会写入策略的 `Comment` 字段，从而在不绘制界面的情况下提供同样的监控信息。

## 实现细节

- **仅使用高阶 API。** 策略通过 `SubscribeLevel1()` 获取最佳买卖价，并利用可配置的蜡烛订阅在市场空闲时触发定时刷新，无需调用低阶连接器接口或自行遍历历史。 
- **账户统计完全对齐。**
  - 当日盈亏等于当前 `PnL` 减去交易日开始时的快照，对应 MQL 中的 `DailyPL()`。
  - 回撤使用 `(Equity - Balance) / Balance * 100%`，与 MetaTrader 的 `(AccountEquity - AccountBalance) / AccountBalance` 一致。
  - “Pos & Orders” 字段合并了当前聚合持仓（`Position`）以及处于 `None`、`Pending`、`Active` 状态的订单数量。
  - 点差由最新的 `Ask - Bid` 计算，若出现负值会被归零以避免显示异常。
- **注释式仪表盘。** 所有统计写入 `Comment`，保持移植版本与原始仪表盘一样只读监控、不触及下单流程。
- **自动日切换。** 一旦 `CurrentTime.Date` 变化，策略会重置当日盈亏基准，确保统计始终针对当前交易日。
- **事件驱动刷新。** 以下事件都会触发 `UpdateDashboard()`：
  - 一级行情更新；
  - 订单状态变化；
  - 持仓变化；
  - 盈亏变化；
  - 可选的定时蜡烛收盘。
  这相当于复现了原始 EA 在 `OnTick()` 中的持续刷新逻辑。

## 参数

| 名称 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `RefreshIntervalSeconds` | 30 | 强制刷新之间的秒数。当值大于零时，会创建对应周期的蜡烛订阅，即使市场没有新报价也会定期更新仪表盘。 |

## 文件结构

- `CS/InformativeDashboardStrategy.cs` – 使用 StockSharp 高阶 API 实现的仪表盘代码。
- `README.md` – 英文说明。
- `README_cn.md` – 中文说明（本文件）。
- `README_ru.md` – 俄文说明。

## 使用提示

1. 在启动策略前指定 `Security` 与 `Portfolio`。如果未设置交易标的，则不会启动定时蜡烛，但一旦收到行情仍会刷新仪表盘。
2. 建议连接支持一级行情的交易接口，以便准确计算点差。
3. 策略仅用于监控，不会调用 `BuyMarket`、`SellMarket` 等交易函数，与原始 MetaTrader 仪表盘保持一致。
