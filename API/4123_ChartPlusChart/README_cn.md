# Chart Plus Chart 策略

## 概述
**Chart Plus Chart 策略** 是 MetaTrader 4 专家顾问 “ChartPlusChart” 的 StockSharp 移植版本。原始 EA 由两个极简脚本组成（`Chart1.mq4` 和 `Chart2.mq4`），它们不会下单，只是不断把最新的收盘价、订单数量、账户余额以及首个持仓的浮动盈亏写入共享 DLL，以便其它图表读取。移植后的版本无需外部 DLL，而是监听一个或两个可配置的 K 线流，并通过类型化的快照事件广播同样的账户信息。

## MQL 版本的核心思想
- 每个 MT4 图表实例都会写入四个数值：`Close[0]`、`OrdersTotal()`、`AccountBalance()`、`OrderProfit()`。
- 脚本没有交易逻辑，它们只负责在初始化和每个 tick 上刷新共享数值。
- 两个实例同时运行，分别使用不同的内存偏移（10 和 70）避免数据冲突。

## StockSharp 实现要点
- 使用高层 API `SubscribeCandles` 订阅 K 线，主数据流必选，辅数据流可通过 `UseSecondaryStream` 开关控制。
- 每当一根 K 线收盘，策略就构建一个 `ChartSnapshot`，其中包含收盘价、活动订单数量、`Portfolio.CurrentValue`（账户权益）以及 `Portfolio.CurrentProfit`（策略盈亏）。
- 快照会缓存在 `PrimarySnapshot`、`SecondarySnapshot` 字段中，并通过 `SnapshotUpdated` 事件对外发布，方便界面或其它策略消费。
- 订单、成交和持仓回调（`OnOrderRegistered`、`OnOrderChanged`、`OnNewMyTrade`、`OnPositionChanged`）会在没有新 K 线时刷新快照，模拟 MT4 脚本持续写入的行为。

## 事件驱动流程
1. 策略启动后订阅所选的 K 线类型。
2. 第一根 K 线收盘时初始化对应快照并触发 `SnapshotUpdated` 事件。
3. 随后的事件（新 K 线或订单/持仓变化）会使用 C# `record struct` 的 `with` 表达式更新现有快照，确保订阅者拿到最新数值。
4. 关闭 `UseSecondaryStream` 后，辅数据流会停止订阅，而主数据流继续工作。

## 快照结构
```csharp
public record struct ChartSnapshot
{
        public decimal LastClose { get; init; }
        public int ActiveOrders { get; init; }
        public decimal AccountBalance { get; init; }
        public decimal TotalProfit { get; init; }
}
```
- **LastClose** 对应 MT4 的 `Close[0]`。
- **ActiveOrders** 映射到 `ActiveOrders.Count`。
- **AccountBalance** 取自 `Portfolio.CurrentValue`，若连接器尚未返回值则为 `0`。
- **TotalProfit** 使用 `Portfolio.CurrentProfit`，提供策略整体浮盈，与 MT4 仅监控第一张订单的值略有差异。

## 参数
| 参数 | 说明 |
|------|------|
| `PrimaryCandleType` | 主数据流跟踪的时间框架或 K 线类型。 |
| `SecondaryCandleType` | 辅数据流的 K 线类型，仅在启用 `UseSecondaryStream` 时使用。 |
| `UseSecondaryStream` | 控制是否订阅第二个数据流并生成辅快照。 |

所有参数都使用 `SetDisplay` 注册了 UI 元数据，可直接在 Designer、Shell 或 Runner 中查看和修改。

## 数据消费方式
```csharp
var strategy = new ChartPlusChartStrategy
{
        PrimaryCandleType = TimeSpan.FromMinutes(15).TimeFrame(),
        SecondaryCandleType = TimeSpan.FromHours(1).TimeFrame(),
        UseSecondaryStream = true
};

strategy.SnapshotUpdated += (stream, snapshot) =>
{
        Console.WriteLine($"[{stream}] close={snapshot.LastClose:0.#####} orders={snapshot.ActiveOrders} " +
                $"balance={snapshot.AccountBalance:0.##} profit={snapshot.TotalProfit:0.##}");
};
```
- 在启动策略之前订阅事件，才能捕获第一次推送。
- 也可以通过 `PrimarySnapshot`、`SecondarySnapshot` 或 `GetSnapshot(stream)` 随时拉取最新值。

## 与 MT4 的差异
- 不再写入共享 DLL，而是通过 C# 事件分发强类型数据，更适合与桌面或 Web 界面集成。
- 采用 `Portfolio.CurrentProfit` 替代 `OrderProfit()`，提供整套策略的浮动盈亏。当存在多张订单时，该值与 MT4 原脚本可能不同。
- 只有在 K 线完成 (`CandleStates.Finished`) 时才刷新快照，以避免未完成的临时数据；MT4 则是在每个 tick 上读取即时收盘价。
- 可以完全关闭辅数据流，在单一图表场景下仍保持工作。

## 使用建议
- 请连接能够更新 `Portfolio.CurrentValue` 和 `Portfolio.CurrentProfit` 的交易柜台或回测环境，这样快照才会反映真实账户状态。
- 若需要可视化，可将 `SnapshotUpdated` 事件绑定到 WPF、WinForms 或 Blazor 等 UI 控件，实现跨图表的统一展示。
- 策略本身不发送交易指令，可与其它交易算法同时运行而互不干扰。
