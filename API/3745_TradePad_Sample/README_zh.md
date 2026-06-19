# TradePad 示例策略

## 概述

**TradePad 示例策略** 移植自 MetaTrader 的 "TradePad" 样例。原始 EA 会在图表上绘制一个符号矩阵，并根据随机指标
(Stochastic) 的读数改变按钮颜色。本移植版本保留了多品种监控和趋势判定的核心逻辑，但不再尝试重现 MT5 中的图形界面。
策略会为每个配置的交易品种订阅蜡烛数据，计算随机指标，并把结果划分为 *Uptrend*、*Downtrend* 或 *Flat* 三种状态。
当状态发生变化时，会输出一条日志消息，对应原版中按钮颜色的切换。

策略不会自动下单，主要定位为人工交易者的监控工具，用于在多个市场之间快速识别动量变化。

## 工作流程

1. **品种解析**：`SymbolList` 参数接受以逗号分隔的代码列表。如果为空，则使用运行环境中分配给策略的主合约。
2. **蜡烛订阅**：所有品种使用 `CandleType` 指定的同一时间框架。
3. **指标计算**：为每个品种创建独立的 `StochasticOscillator` 指标，在蜡烛收盘时得到 `%K` 值。
4. **趋势判定**：读数高于 `UpperLevel` 视为 *Uptrend*，低于 `LowerLevel` 视为 *Downtrend*，其余情况视为 *Flat*。最近的
   `%K` 值保存在 `LatestKValues` 中。
5. **刷新节流**：`TimerPeriodSeconds` 参数模拟原始 TradePad 的定时器逻辑，每个品种在该间隔内最多记录一次状态变更，避免
   高频蜡烛导致日志泛滥。

## 参数

| 参数 | 说明 |
|------|------|
| `SymbolList` | 要监控的交易品种，逗号分隔；留空时使用策略的主合约。 |
| `TimerPeriodSeconds` | 每个品种之间隔多少秒才允许再次记录状态，用于节流。 |
| `StochasticLength` | 随机指标 `%K` 的基准周期长度。 |
| `StochasticKPeriod` | `%K` 线的平滑周期。 |
| `StochasticDPeriod` | `%D` 线的平滑周期（保留以方便优化，策略目前只读取 `%K`）。 |
| `UpperLevel` | 判定为上涨趋势的阈值。 |
| `LowerLevel` | 判定为下跌趋势的阈值。 |
| `CandleType` | 用于计算指标的蜡烛时间框架。 |

## 使用提示

- 请确保连接器能够提供参数中列出的所有品种。不存在的代码会在日志中提示并被跳过。
- `TrendStates` 属性公开了最新的趋势分类，便于在 Designer 中绑定自定义可视化组件。
- 可以在 Designer 或 Runner 中结合自定义界面，将 `AddInfoLog` 消息或公开字典映射到面板、小工具等。
- 策略不发送任何订单，可在真实行情连接上安心用于监控。

## 原版 MQL 与 StockSharp 版本的差异

| MQL5 功能 | StockSharp 中的实现 |
|-----------|---------------------|
| 图形按钮面板 | 以日志与公开字典的形式提供，方便在 Designer 中自建界面。 |
| BUY/SELL 按钮 | 未实现，策略保持纯监控模式。 |
| 拖动图表逻辑 | 与 StockSharp 不兼容，已省略。 |
| 趋势颜色刷新 | 通过 `TimerPeriodSeconds` 控制的状态更新代替。 |

## 扩展建议

- 在 Designer 中读取 `TrendStates`，利用自定义控件恢复彩色矩阵效果。
- 当某个品种从 *Flat* 转为 *Uptrend* 或 *Downtrend* 时触发提醒、推送或声音提示。
- 若需自动交易，可基于当前分类叠加下单逻辑或风险控制模块。
