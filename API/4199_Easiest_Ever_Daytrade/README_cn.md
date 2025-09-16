# Easiest Ever Daytrade 策略

## 概览
- 将 MetaTrader 4 专家顾问 **“Easiest ever - daytrade robot”** 转换为 StockSharp 高级 API 实现。
- 属于简化的日内策略：每个交易日最多开立一笔仓位，方向取决于上一根日线的收盘与开盘关系。
- 策略完全依赖 K 线数据，不使用技术指标或振荡器，所有操作均为市价单。

## 交易逻辑
1. 订阅日线数据（`DailyCandleType`，默认 `TimeSpan.FromDays(1)`），保存最近一根已完成日线的开盘价与收盘价。
2. 订阅日内 K 线（`IntradayCandleType`，默认 `TimeSpan.FromMinutes(1)`），驱动进出场流程。
3. 在早盘时段（当当前 K 线开盘小时数严格小于 `EntryHourLimit`，默认 `1`）执行：
   - 若前一根日线收盘价高于开盘价，则调用 `BuyMarket(TradeVolume)` 开多单。
   - 若前一根日线收盘价低于开盘价，则调用 `SellMarket(TradeVolume)` 开空单。
   - 若开盘价等于收盘价，则跳过当日交易。
4. 仓位持有至收盘。当当前 K 线的小时数大于或等于 `MarketCloseHour`（默认 `20`）时，使用市价单强制平仓（多单用 `SellMarket`，空单用 `BuyMarket`）。
5. 策略仅在无持仓时允许再次进场，因此每日最多执行一次交易。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `TradeVolume` | 多空共用的下单数量，必须为正数。 | `1` |
| `EntryHourLimit` | 允许开仓的最后小时（不包含该小时），有效范围 `[0, 23]`。 | `1` |
| `MarketCloseHour` | 每日强制平仓的小时数。 | `20` |
| `IntradayCandleType` | 用于执行逻辑与仓位管理的时间框。 | `TimeSpan.FromMinutes(1).TimeFrame()` |
| `DailyCandleType` | 用于读取上一交易日开盘与收盘的时间框。 | `TimeSpan.FromDays(1).TimeFrame()` |

所有参数均通过 `Param()` 注册，可在 StockSharp 优化器中调参。

## 风险控制
- 策略不设止损或止盈，风险通过在 `MarketCloseHour` 平仓来限制。
- 在 `OnStarted` 中调用 `StartProtection()`，确保意外持仓受到监控。
- 由于每天最多持有一笔仓位，总风险敞口由 `TradeVolume` 决定。

## 使用建议
- 需要同时提供日内与日线历史数据，默认配置要求分钟级别和日级别 K 线。
- 根据标的交易时段调整 `EntryHourLimit` 与 `MarketCloseHour`，以匹配实际交易时间。
- 假定 K 线时间戳与交易所本地时间一致，如有偏差需使用相应的时区数据源。
- 策略忠实复刻原始 MQL 专家逻辑，可在 StockSharp 生态中复用而无需 Python 版本。
