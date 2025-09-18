# MultiTimeframeEmaAlignmentStrategy

## 概述
**MultiTimeframeEmaAlignmentStrategy** 是来自 `MQL/7713` 目录下 `1h-4h-1d.mq4` EA 的 StockSharp 版本。原始 EA 通过在三个时间框架上比较快速与慢速指数移动平均线（EMA），并结合固定止损、止盈和移动止损来管理风险。本策略沿用这一核心思想，但使用 StockSharp 的高阶 API 完成指标绑定与交易管理。

## 交易逻辑
- 同时订阅三个时间框架：M1 作为信号周期、M5 作为中周期过滤、M30 作为趋势确认周期。
- 每个周期都会计算一对 EMA（可配置，默认 8 与 64）。
- **做多条件**：三个时间框架中的快速 EMA 都需位于慢速 EMA 之上，同时快速 EMA 不能出现动能下降（当前值必须不低于上一根 M1 K 线以及 `ShiftDepth` 根之前的值）。
- **做空条件**：三个时间框架中的快速 EMA 都需位于慢速 EMA 之下，且快速 EMA 需要保持下降动能。
- 满足条件时，在 M1 K 线收盘处下单。若已持有反向仓位会先平仓，再开新仓。

`ShiftDepth` 参数用于仿真 MT4 中 "MA shift" 的比较，记录若干根历史 EMA 数值，确保动能判断与原始脚本一致。

## 风险控制
- `TradeVolume` 控制下单手数（默认 3，和原始 EA 一致）。
- 止损、止盈距离以点数配置，并通过交易品种的 `PriceStep` 转换为价格。若 `PriceStep` 不可用，会退化为 `0.0001`。
- 移动止损会在价格向盈利方向运行时自动上移/下移止损价。
- 可分别启用/禁用止损、止盈、移动止损，对应原脚本中的 `StopLossMode`、`TakeProfitMode` 与 `TrailingStopMode`。

## 参数说明
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `TradeVolume` | 市价单手数。 | `3` |
| `FastLength` | 快速 EMA 的周期。 | `8` |
| `SlowLength` | 慢速 EMA 的周期。 | `64` |
| `ShiftDepth` | 回溯 EMA 数值的根数，用于模拟 MT4 的 MA shift 比较。 | `3` |
| `UseStopLoss` | 是否启用固定止损。 | `true` |
| `StopLossPips` | 固定止损距离（点）。 | `75` |
| `UseTakeProfit` | 是否启用固定止盈。 | `true` |
| `TakeProfitPips` | 固定止盈距离（点）。 | `150` |
| `UseTrailingStop` | 是否启用移动止损。 | `true` |
| `TrailingStopPips` | 移动止损距离（点）。 | `30` |
| `M1CandleType` | 信号周期的 K 线类型。 | `1m` |
| `M5CandleType` | 中周期过滤的 K 线类型。 | `5m` |
| `M30CandleType` | 高周期趋势确认的 K 线类型。 | `30m` |

## 使用提示
1. 运行前需保证三个时间框架都有足够的历史数据，以便 EMA 缓冲区可以形成。
2. `ShiftDepth` 建议保持在 `2` 以上，以免动能判断失效。
3. 当仅启用移动止损而关闭固定止损时，移动止损会在仓位盈利后自动创建止损价。
4. StockSharp 按 K 线收盘触发信号，与 MT4 的逐笔执行相比可能存在细微差异，尤其在波动剧烈的行情中。

## 转换说明
- 指标计算完全通过 `Bind` 完成，没有自行管理指标序列。
- 下单使用 `BuyMarket` / `SellMarket` 等高阶接口替代 MT4 的 `OrderSend`。
- 原脚本中的邮件提醒、滑点设置等功能未移植，因为它们超出本次转换范围。

## 文件列表
- `CS/MultiTimeframeEmaAlignmentStrategy.cs` —— 策略主文件。
- `README.md` —— 英文说明。
- `README_ru.md` —— 俄文说明。
