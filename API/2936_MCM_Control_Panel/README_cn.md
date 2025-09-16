# MCM 控制面板监控策略
[English](README.md) | [Русский](README_ru.md)

## 概述

**MCM 控制面板监控策略** 将原始的 MetaTrader 示例移植到 StockSharp。策略不会绘制界面，而是为指定的品种订阅一个或多个蜡烛图时间框以及逐笔行情，并在每次事件到来时向日志输出详细信息。它适用于多时间框诊断、行情源验证以及构建自定义监控面板。

策略完全只读，不会发送任何订单。它通过日志复现 MQL 控制面板的行为，把所有事件转换成易读的文字。

## 工作流程

1. 启动后策略首先订阅主时间框的蜡烛，如果启用，也会订阅第二和第三个时间框。每个时间框都可以设置为数据源支持的任意周期。
2. 每根完成的蜡烛都会触发一条日志，包含品种、时间框标识（M1、H4、D1 等）、收盘价、成交量以及事件时间，格式与原 MQL 面板一致。
3. 如果启用了 *Log Unfinished Candles*，策略会在新蜡烛开始形成时立即记录更新，方便实时监控正在生成的蜡烛。
4. 启用 *Track Ticks* 后，策略还会监听逐笔成交并输出价格、数量和时间，相当于复现 `CHARTEVENT_TICK` 事件。
5. 所有订阅均通过 StockSharp 的高级 API (`SubscribeCandles`、`SubscribeTicks`) 创建，可直接在 Designer、Shell 或自研主机中使用。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| **Primary Timeframe** | 必须监控的主时间框。 | 5 分钟蜡烛 |
| **Use Secondary Timeframe** | 启用第二个蜡烛序列。 | 关闭 |
| **Secondary Timeframe** | 可选的第二时间框。 | 15 分钟蜡烛 |
| **Use Tertiary Timeframe** | 启用第三个蜡烛序列。 | 关闭 |
| **Tertiary Timeframe** | 可选的第三时间框。 | 1 小时蜡烛 |
| **Track Ticks** | 监听逐笔成交并记录日志。 | 开启 |
| **Log Unfinished Candles** | 记录尚未完成的蜡烛，便于捕捉新蜡烛开始时间。 | 关闭 |

## 使用建议

- 启动前请先指定 `Security`。日志会输出 `Security.Id`，便于在 StockSharp 的日志查看器中筛选。
- 策略不会下单，可安全地与实际交易策略同时运行，用作行情监控。
- 在 Designer 中可以打开多个日志面板，以实时观察不同时间框的同步情况。
- 想要模拟原始控制面板，可以对不同品种启动多个实例，或在单个实例中启用多个时间框。
- 需要精确获知新蜡烛开始时间时，打开 *Log Unfinished Candles*；若只关心已确认的收盘，可保持关闭。

## 与原 MQL 程序的对应关系

| MQL 组件 | StockSharp 中的实现 |
|----------|--------------------|
| `InitControlPanelMCM` 颜色和字体设置 | 使用策略参数替代，日志系统负责呈现。 |
| `OnChartEvent` 处理 `CHARTEVENT_CUSTOM` | 针对每个时间框的蜡烛订阅，并在日志中打印时间框标签。 |
| `CHARTEVENT_TICK` | 可选的逐笔订阅，日志格式与蜡烛事件一致。 |
| `TimeToString(...) -> id=...` 输出 | `AddInfoLog` 提供品种、时间框、价格、成交量和时间戳。 |

## 日志示例

```
[EURUSD] M5 closed candle price=1.09845 volume=27 time=2024-03-05T09:35:00.0000000Z
[EURUSD] Tick price=1.09852 volume=1 time=2024-03-05T09:35:07.2510000Z
```

这些日志说明策略收到了五分钟蜡烛的收盘以及随后的一笔成交，与 MQL 控制面板提供的信息相同。
