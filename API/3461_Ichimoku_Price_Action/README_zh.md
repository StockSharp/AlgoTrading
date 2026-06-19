# Ichimoku Price Action Strategy

## 概述
**Ichimoku Price Action Strategy** 是将 MQL4 专家顾问“Ichimoku Price Action Strategy v1.0”移植到 StockSharp 高级 API 的版本。原始 EA 只要允许交易并且可选的 MACD 过滤器给出方向，就会在当前时间段内直接市价开仓。本移植版沿用了相同的思想，同时在 C# 侧实现了完整的风控：止损、止盈、保本以及移动止损。

策略适用于希望自动化日内时间段交易的交易者。所有信号都在选定交易周期的收盘价上进行评估；若启用 ATR 或摆动高低点止损，则会自动订阅额外周期的数据。

> **重要说明：** StockSharp 策略只跟踪净持仓，因此不会像原始模版那样同时持有多单与空单。其他资金管理逻辑通过每根完成 K 线上的止损/止盈/移动止损计算来实现。

## 交易逻辑
1. **时间过滤** – 仅当当前时间位于 `[StartTime; EndTime]` 区间内时才允许开仓。两者都设为 `00:00` 可关闭该过滤。
2. **MACD 确认（可选）** – `UseMacdFilter = true` 时，多头需要 MACD 主线高于信号线，空头则需要相反。
3. **下单** – 当方向被允许且当前无持仓时，以 `Volume` 指定的数量发送市价单。
4. **止损** – 根据 `StopLossMode` 选择固定点数、ATR 倍数或最近摆动高低点；每根 K 线都会重新计算，若新水平更保守则收紧止损。
5. **止盈** – 可选择固定点数或基于当前风险距离的收益比目标，满足条件后即刻市价平仓。
6. **保本与移动止损** – 盈利达到 `MoveToBreakEven` 后将止损推至开仓价；盈利达到 `TrailingTrigger` 后启动移动止损，按照 `TrailingStop` 与 `TrailingStep` 保持利润。
7. **反向信号出场** – `CloseOnReverse = true` 时出现反向入场条件会立即平掉当前持仓。

## 风险控制
- **止损**
  - *固定点差* – 使用 `StopLossPips` × `PriceStep`。
  - *ATR 倍数* – 使用 `AtrCandleType` 的 ATR × `AtrMultiplier`。
  - *摆动高低点* – 使用 `SwingCandleType` 与 `SwingBars` 计算出的最近高/低点。
- **止盈**
  - *固定点差* – 使用 `TakeProfitPips`。
  - *风险收益比* – 使用当前止损距离 × `TakeProfitRatio`。
- **保本** – `MoveToBreakEven` 指定需要的盈利点数，达到后止损移动到开仓价。
- **移动止损** – `TrailingStop`、`TrailingTrigger`、`TrailingStep` 控制移动止损的距离、触发条件与步进。

## 参数一览
| 组别 | 参数 | 说明 |
| --- | --- | --- |
| General | `BuyMode` | 是否允许做多。 |
| General | `SellMode` | 是否允许做空。 |
| General | `CandleType` | 信号使用的主周期（默认 1 小时）。 |
| Schedule | `StartTime` / `EndTime` | 交易时段（00:00 表示禁用）。 |
| Filters | `UseMacdFilter` | 是否启用 MACD 过滤。 |
| Filters | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD 三个周期参数。 |
| Risk | `StopLossMode` | 止损模式：`FixedPips`、`AtrMultiplier`、`SwingHighLow`。 |
| Risk | `StopLossPips` | 固定止损点数。 |
| Risk | `AtrMultiplier`, `AtrPeriod`, `AtrCandleType` | ATR 止损相关设置。 |
| Risk | `SwingBars`, `SwingCandleType` | 摆动高低点止损设置。 |
| Risk | `TakeProfitMode` | 止盈模式：`FixedPips` 或 `RiskReward`。 |
| Risk | `TakeProfitPips`, `TakeProfitRatio` | 止盈点数或风险收益比。 |
| Risk | `CloseOnReverse` | 是否在反向信号时立即离场。 |
| Orders | `Volume` | 每次下单的数量。 |
| Risk | `MoveToBreakEven` | 保本触发点数。 |
| Risk | `TrailingStop`, `TrailingTrigger`, `TrailingStep` | 移动止损配置。 |

## 使用建议
- 确保标的物定义了 `PriceStep`，否则默认使用 0.0001 作为点值。
- 启用 ATR 或摆动止损时需要行情源提供对应周期的数据。
- 将保本和移动止损相关参数设为 0 即可禁用这些功能。
- 策略默认不会累加同方向仓位，只有在平仓后才会寻找下一次入场机会。

## 与 MQL 版本的差异
- 由于 StockSharp 限制，只支持净持仓，无法同时持有多单和空单。
- Kelly 等复杂资金管理、分批止盈等功能未包含在移植版中。
- 原始模版的人工确认、面板显示、截图等附加功能被省略。

## 回测清单
1. 设定主交易周期以及所需的 ATR/摆动周期。
2. 根据原始 EA 调整 `Volume`、止损与止盈参数。
3. 根据需要启用或关闭 MACD 过滤。
4. 确认交易时段与原始测试一致后启动回测。
5. 检查日志以确认止损、止盈、移动止损事件触发正常。
