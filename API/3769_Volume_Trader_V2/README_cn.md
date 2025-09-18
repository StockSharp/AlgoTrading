# Volume Trader V2 策略

## 概述
Volume Trader V2 是 MetaTrader 专家顾问 `Volume_trader_v2_www_forex-instruments_info.mq4` 的直接移植版本。原始系统通过观察最近蜡烛的总成交量变化来判断短期资金流向，并据此保持单一方向的仓位。本次移植保留了仅持有单仓、按时间窗口过滤以及每根完成蜡烛只处理一次的行为特征。

策略会订阅一个可配置的蜡烛序列，并缓存最近两根完成蜡烛的成交量。当新蜡烛收盘时，会比较前两根蜡烛的成交量（即 MetaTrader 中的 `Volume[1]` 和 `Volume[2]`），并生成最新的交易方向：

- 当 `Volume[1] < Volume[2]` 时产生 **做多** 信号。
- 当 `Volume[1] > Volume[2]` 时产生 **做空** 信号。
- 成交量相等或不在允许的交易时间内，则平掉所有仓位。

在发送新订单之前，如果当前仓位方向相反，会先平仓，以确保 StockSharp 版本与 MetaTrader 的订单生命周期保持一致。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | 5 分钟周期 | 通过 `SubscribeCandles` 请求的数据类型，请根据原始图表周期进行调整。 |
| `StartHour` | 8 | 允许交易的起始小时（包含）。在此时间段之外会忽略信号并关闭仓位。 |
| `EndHour` | 20 | 允许交易的结束小时（包含）。蜡烛起始时间超过该值时策略保持空仓。 |
| `TradeVolume` | 0.1 | 从 EA 复制的下单手数，同时写入 `Strategy.Volume` 供辅助下单方法使用。 |

所有参数都通过 `StrategyParam<T>` 暴露，可用于界面配置或参数优化。

## 交易逻辑
1. 仅处理已完成的蜡烛，确保与 EA 的逐根逻辑保持一致。
2. 在计算信号前，将 `Volume[1]` 与 `Volume[2]` 的对应值缓存到 `_previousVolume` 和 `_twoBarsAgoVolume`。
3. 检查蜡烛的开始时间是否处于 `StartHour` 与 `EndHour` 之间（包含端点）。若不在范围内，则立即平仓并跳过开仓。
4. 根据成交量比较得出目标方向：
   - 最新成交量小于上一根时做多。
   - 最新成交量大于上一根时做空。
   - 其余情况视为中性。
5. 当目标方向与当前仓位不一致时，先通过 `BuyMarket(-Position)` 或 `SellMarket(Position)` 平掉反向仓位。
6. 仅在当前为空仓或刚刚完成反向平仓时，使用配置的 `TradeVolume` 开启新仓。
7. 更新缓存的成交量，以便下一次循环继续比较最近两根完成蜡烛。

上述流程确保蜡烛尚未收盘时不会产生订单，并保持与依赖 `LastBarChecked` 的 MetaTrader 实现同样的节奏。

## 补充说明
- 在 `OnStarted` 中调用 `StartProtection()`，利用框架的仓位保护辅助工具追踪当前仓位。
- `Comment` 属性会输出与 EA 相同的提示信息（`"Up trend"`、`"Down trend"`、`"No trend..."`、`"Trading paused"`），方便监控。
- 策略未引入额外集合，完全使用高层蜡烛订阅 API，符合项目规范。
- 请根据原始 EA 使用的品种和周期，设置合适的蜡烛类型、标的与手数，以获得可比的表现。
