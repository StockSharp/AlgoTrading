# Magic Number Wise EA Profit/Loss Dashboard 策略（C#）

此目录包含 MetaTrader 5 工具 **Magic-Number-wise-EA-Profit-Loss-Live-Dashboard_v1** 的 StockSharp 版本。原始 EA 会遍历交易历史、按魔术编号分组并在图表上绘制实时表格。移植版保留了定时聚合逻辑，并通过策略日志输出同样的信息，方便在 Designer 或任何 StockSharp 宿主中观察。

## 功能概览

* 监听策略组合里出现的所有订单与成交。
* 维护一张按标识符分组的内存表：
  * 默认使用 `Order.UserOrderId`（MT 机器人常把魔术编号写在这里）。
  * 也可以改用订单注释字符串，兼容只传递注释的桥接程序。
* 对每个标识符统计：
  * 已执行成交次数。
  * StockSharp 在 `MyTrade` 上提供的累计已实现盈亏。
  * 来自订单或成交的最新品种代码。
  * 可选的浮动盈亏（来源于当前 `Portfolio.Positions` 中相同品种的持仓）。
* 以固定频率输出格式化快照，表头与 MT5 面板保持一致：`Magic Id | Deals | Closed P/L | Floating P/L | Symbol | Comment`。

由于 StockSharp 对同一品种使用净头寸模型，浮动盈亏会按品种匹配到相应行。只要每个魔术编号控制一个品种，该行为就与原脚本一致，契合常见的 MT 对冲账户设置。

## 参数

所有参数通过 `Param()` 创建，可在 Designer 中调整或优化。

| 参数 | 说明 |
|------|------|
| `RefreshInterval` | 刷新仪表盘的时间间隔（默认 5 秒）。|
| `GroupByComment` | 设为 `true` 时按 `Order.Comment` 分组，适用于不回传 `UserOrderId` 的经纪商。|
| `IncludeOpenPositions` | 启用后，会把 `Portfolio.Positions` 提供的浮动盈亏附加到汇总行。|

## 工作流程

1. 启动策略时会检查刷新周期，并创建 `System.Threading.Timer` 立即触发第一次报告。
2. `OnOrderRegistered` 与 `OnOrderChanged` 在订单出现时就登记新的标识符，同时保存品种和注释信息。
3. `OnNewMyTrade` 递增成交数量，并累加 StockSharp 返回的已实现盈亏。
4. 每次定时器触发：
   * 重置浮动盈亏，若启用则同步最新的 `Portfolio.Positions`。
   * 复制当前统计数据、按标识符排序，并将表格打印到日志。

移植版沿用了 MT5 仪表盘的思路，只是用日志代替了图表文本，既能在回测中运行，也能在实盘中保持确定性。

## 使用建议

1. 将策略连接到需要监控的账户（无需订阅行情）。
2. 如果桥接程序把 EA 标识写在订单注释里，请把 `GroupByComment` 设为 `true`。
3. 保持 `IncludeOpenPositions` 为开启状态即可在已实现盈亏旁看到浮动盈亏；若适配器暂不提供 `Position.PnL` 可将其关闭。
4. 观察日志窗口，每次刷新都会输出与 MT5 面板对齐的表格。

## 文件

* `CS/MagicNumberWiseEaProfitLossDashboardStrategy.cs` – 策略实现代码。
* `README.md` – 英文说明。
* `README_cn.md` – 中文说明。
* `README_ru.md` – 俄文说明。
