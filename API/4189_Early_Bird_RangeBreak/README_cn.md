# 早鸟区间突破策略

## 概述
**早鸟区间突破策略** 是 MetaTrader "earlyBird3" 专家顾问的 C# 版本。策略关注欧洲时段开盘后形成的早盘整理区间，使用 14 周期 RSI 作为趋势过滤，在价格向上或向下突破区间时立即分批进场（默认 3 笔市价单）。每笔仓位都绑定预设止盈、统一止损，并在波动度放大时启用可选的移动止损。

## 数据要求
- 仅需一种时间框架的K线（默认 5 分钟）。
- 交易标的必须提供有效的 `PriceStep`，因为所有止损/止盈都以点数表示。
- 交易时间依据接收到的K线时间戳（即数据源服务器时间）。

## 交易时段
1. **区间构建**：在 `RangeStartHour` 到 `RangeEndHour` 之间记录最高价和最低价。
2. **交易窗口**：从 `TradingStartHour:TradingStartMinute` 开始到 `TradingEndHour` 之前启动突破逻辑。
3. **强制平仓**：到达 `ClosingHour` 时，不论盈亏都会平掉剩余仓位。
4. **仅限工作日**：只在周一到周五处理信号。

## 入场逻辑
1. 多头突破价 = 区间高点 + `EntryBufferPoints`，空头突破价 = 区间低点 − `EntryBufferPoints`（均为点数）。
2. RSI>50 才允许多头，RSI≤50 才允许空头。
3. 每个方向每天只触发一次，触发后立即下 3 笔市价单（默认每笔 0.1 手）。
4. 如果存在反向仓位且 `HedgeTrading` 为假，则忽略新信号；若为真，会先平掉旧仓位再建立新方向。由于 StockSharp 账户使用净头寸，此实现采用“平仓后反手”的方式来模拟原策略的对冲行为。

## 离场与风控
1. **止损**：`StopLossPoints` 定义的点差会转换为价格，若触发立即关闭剩余头寸。
2. **分级止盈**：`TakeProfit1Points`、`TakeProfit2Points`、`TakeProfit3Points` 依次关闭一部分仓位，最后一部分继续持有直到触发止损、移动止损或交易结束。
3. **移动止损**：仅在最后一部分仓位剩余时启用。当前K线振幅必须大于 `ATR * TrailingRiskMultiplier`，且价格至少朝盈利方向走出 `TrailingStopPoints`，才会上调止损，使其始终保持原始止损距离。
4. **收盘平仓**：时间达到 `ClosingHour` 时立即清空所有仓位。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `AutoTrading` | 是否启用自动下单。 | `true` |
| `HedgeTrading` | 是否允许在出现反向信号时反手（先平仓再反向）。 | `true` |
| `OrderType` | `0`=双向，`1`=仅多，`2`=仅空。 | `0` |
| `TradeVolume` | 每笔市价单的手数。 | `0.1` |
| `StopLossPoints` | 止损点差。 | `60` |
| `TakeProfit1Points` | 第一目标止盈点差。 | `10` |
| `TakeProfit2Points` | 第二目标止盈点差。 | `20` |
| `TakeProfit3Points` | 第三目标止盈点差。 | `30` |
| `TrailingStopPoints` | 移动止损启动所需的最小盈利点差。 | `15` |
| `TrailingRiskMultiplier` | 波动度过滤的 ATR 倍数。 | `1.0` |
| `EntryBufferPoints` | 突破价外加的缓冲点数。 | `2` |
| `RangeStartHour` | 区间统计起始小时。 | `3` |
| `RangeEndHour` | 区间统计结束小时。 | `7` |
| `TradingStartHour` | 允许入场的起始小时。 | `7` |
| `TradingStartMinute` | 允许入场的起始分钟。 | `15` |
| `TradingEndHour` | 停止开新仓的小时。 | `15` |
| `ClosingHour` | 强制平仓的小时。 | `17` |
| `RsiPeriod` | RSI 观察期。 | `14` |
| `VolatilityPeriod` | ATR 观察期。 | `16` |
| `CandleType` | 使用的K线类型（默认 5 分钟）。 | `TimeSpan.FromMinutes(5)` |

## 实现要点
- 使用 StockSharp 高层 API 订阅K线，并通过 `Bind` 同时绑定 RSI 与 ATR 指标。
- 所有指标值直接在 `ProcessCandle` 中处理，未调用 `GetValue()` 或额外缓存，符合项目规范。
- 仅处理完成态的K线，忽略未结束的更新。
- 点数通过 `Security.PriceStep` 转换为实际价格，配置前请确认品种的最小跳动单位。
- 原始 EA 通过多笔单子实现对冲，本移植版本在 `HedgeTrading` 打开时采用先平后反的方式，以适配净头寸账户。

## 使用建议
- 根据所用市场的时区调整 `RangeStartHour`、`RangeEndHour` 与交易窗口，保持与原 MT4 策略相同的早盘区间。
- 优化时可重点关注突破缓冲、止盈阶梯以及波动度过滤，它们决定了假突破与错失行情之间的平衡。
- 若希望更紧凑的移动止损，可降低 `TrailingRiskMultiplier` 或 `StopLossPoints`，让止损更快贴近价格。

