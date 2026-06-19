# ReInitChart 策略
[English](README.md) | [Русский](README_ru.md)

该策略把 MetaTrader 的 **ReInitChart** 工具移植到 StockSharp。原始脚本会在每个图表上放置一个按钮，并通过临时切换时间框架来迫使平台重新计算指标。StockSharp 版本通过 `ManualRefreshRequest` 参数与可选的 `AutoRefreshEnabled` 计时器保留这种思想：它们重置内部的 SMA 指标并在日志中记录刷新事件，同时示例性地使用简单的均线趋势策略来展示刷新后的交易行为。

## 工作流程

1. **主数据源** —— 根据 `CandleType` 订阅蜡烛，并计算长度为 `SmaLength` 的简单移动平均。
2. **手动刷新** —— 当 `ManualRefreshRequest` 被设为 `true` 时，移动平均被重置，标志自动恢复为 `false`，并在日志中写入原始按钮的元数据（`RefreshCommandName`、`RefreshCommandText`、`TextColorName`、`BackgroundColorName`）。
3. **自动刷新** —— 启用 `AutoRefreshEnabled` 后，策略会按照 `AutoRefreshInterval` 周期性地再次重置指标，复现 MetaTrader 中基于定时器的重新初始化。
4. **交易逻辑** —— 在 SMA 成形之后，策略最多只持有一个方向的仓位：收盘价高于均线时做多，跌破均线时先平掉多头再做空。

这样就用 StockSharp 的原生机制（指标重置与日志）达到了重新初始化所有图表的效果，无需在时间框架之间来回切换。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 用于订阅市场数据的时间框架。 |
| `SmaLength` | 每次刷新后重新计算的移动平均长度。 |
| `AutoRefreshEnabled` | 是否启用周期性刷新。 |
| `AutoRefreshInterval` | 自动刷新的时间间隔。 |
| `ManualRefreshRequest` | 手动设置为 `true` 以立即刷新，策略处理完毕后会自动清零。 |
| `RefreshCommandName` | 保留自 MetaTrader 的按钮名称，刷新时写入日志。 |
| `RefreshCommandText` | 保留自 MetaTrader 的按钮标题，刷新时写入日志。 |
| `TextColorName` | 按钮文字颜色的描述，方便追溯与记录。 |
| `BackgroundColorName` | 按钮背景颜色的描述，方便追溯与记录。 |

## 使用方法

1. 根据需要设置 `CandleType` 和 `SmaLength`。
2. 如果需要定期重新初始化，启用 `AutoRefreshEnabled` 并调整 `AutoRefreshInterval`；若只需手动控制，则保持关闭。
3. 当需要刷新计算结果时，把 `ManualRefreshRequest` 改为 `true`。策略会自动将其恢复为 `false`，并从下一根蜡烛开始重新积累数据。
4. 启动策略。它会订阅市场数据、绘制蜡烛和 SMA 曲线以及成交记录，并在指标准备就绪后执行简单的趋势跟随交易。

## 与原始 MQL 脚本的差异

- StockSharp 没有图表按钮这一界面元素，因此刷新触发通过策略参数实现。
- 不再通过在 M1 与 M5 之间切换时间框架，而是直接重置指标，这更符合 StockSharp 的工作方式。
- 按钮的名称和颜色仅作为日志元数据被保留，策略不会在图表上创建额外的控件。
