# SurefireThing 策略

## 概述
SurefireThing 策略是 MetaTrader 4 专家顾问 *Surefirething* 的 StockSharp 高级 API 移植版本。策略仅在收盘后的完整 K 线数据上工作，根据前一交易日的价格区间计算挂单水平，并在每天结束时清空持仓。核心思想是利用前收盘价周围的对称限价挂单，捕捉可能的均值回归。

## 交易逻辑
- 每当检测到新的交易日，策略会尝试平掉当前仓位并撤销仍然有效的挂单。
- 取上一交易日最后一根完成的 K 线，计算其范围 `(High - Low)` 并乘以 `RangeMultiplier`（默认 1.1，与原版 EA 相同）。
- 将调整后的范围的一半加到前收盘价上得到卖出限价单价位，同样的距离减去前收盘价得到买入限价单价位。
- 止损与止盈参数以价格最小变动单位表示。当品种提供有效的 `Security.Step` 时，策略会把这些距离转换为绝对价格并通过 `StartProtection` 自动创建保护单。
- 每个交易日仅提交一次挂单。若挂单成交则由保护单负责离场，否则订单会一直保留到下一次日内重置。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 每次挂单的下单量。 | `0.1` |
| `TakeProfitPoints` | 止盈距离，单位为价格步长。若可用则自动换算为绝对价格。 | `10` |
| `StopLossPoints` | 止损距离，单位为价格步长。换算方式与止盈相同。 | `15` |
| `RangeMultiplier` | 用于放大前一根 K 线区间的倍数。 | `1.1` |
| `CandleType` | 策略处理的主要时间框架。默认使用 1 分钟 K 线，可按需要调整以匹配原始图表。 | `TimeSpan.FromMinutes(1)` |

## 实现细节
- **高级 API**：通过 `SubscribeCandles(CandleType)` 订阅 K 线，只在 `ProcessCandle` 中处理已完成的 K 线。
- **日内重置**：利用 K 线时间戳判断是否跨日，`CloseForNewDay` 在新交易日开始时关闭仓位并撤单。
- **保护逻辑**：`ConfigureProtection` 将点值参数转换为 `Unit`，并启用 `StartProtection`，使得成交后自动生成止损/止盈保护单。
- **订单管理**：使用 `_buyLimitOrder` 与 `_sellLimitOrder` 保存挂单引用，在 `CancelPendingOrder` 及 `OnOrderChanged` 中清理已完成或撤销的订单。
- **价格规整**：在提交订单前通过 `Security.ShrinkPrice` 将计算出的价格收敛到品种的最小跳动单位。

## 使用建议
- 根据原版 EA 所使用的图表时间框架调整 `CandleType`，以保持相同的参考 K 线。
- 若标的波动率差异较大，可调节 `RangeMultiplier` 以控制挂单距离。
- 如果经纪商限制最小止损距离，请确认 `TakeProfitPoints` 与 `StopLossPoints` 在换算为绝对价格后满足规则。
- 策略假定存在连续的日内数据；遇到周末或假期缺口时，会在下一根可用 K 线上完成重置并重新挂单。
