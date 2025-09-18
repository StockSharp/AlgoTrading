# Pipsover 8167 策略

## 概述
**Pipsover 8167** 是 MetaTrader 4 智能交易系统 `Pipsover.mq4`（8167 版本）的 StockSharp 移植版本。原始脚本在上一根 K 线回撤到 20 周期简单移动平均线之后，寻找 Chaikin 振荡器的强烈尖峰，并按照冲量方向开仓，同时固定止损 70 点、止盈 140 点。本 C# 实现完全复刻该逻辑，使用 StockSharp 的高层 API，无需直接访问指标缓冲区。

策略通过累积/派发线（ADL）以及两条指数移动平均线重新构建 Chaikin 振荡器，以复制 `iCustom("Chaikin", ...)` 在 MetaTrader 中的输出。所有交易判断都在 K 线收盘后完成，对应原脚本中的 `OrdersTotal()` 以及 `Close[1]` / `Open[1]` 检查。

## 指标与信号
- **简单移动平均线（SMA 20）**：基于收盘价计算。上一根 K 线的最低价必须跌破 SMA（做多），或最高价突破 SMA（做空），同时保持与信号方向一致的实体。
- **Chaikin 振荡器（EMA 3 – EMA 10 of ADL）**：利用 ADL 数据流重建，输出值与 MetaTrader 中的 `iCustom("Chaikin", 0, 0, 1)` 相同。入场与离场阈值都以振荡器的绝对值表示。
- **价格行为过滤器**：读取上一根 K 线的实体方向。阳线允许做多，阴线允许做空。

## 交易规则
### 多头开仓
1. 上一根 K 线收盘价高于开盘价（`Close[1] > Open[1]`）。
2. 上一根最低价低于该 K 线的 SMA20。
3. 上一根 Chaikin 值低于 `-OpenLevel`（默认 55）。
4. 当前没有持仓。

### 空头开仓
1. 上一根 K 线收盘价低于开盘价（`Close[1] < Open[1]`）。
2. 上一根最高价高于该 K 线的 SMA20。
3. 上一根 Chaikin 值高于 `OpenLevel`。
4. 当前没有持仓。

### 平仓规则
- **多单**：下一根 K 线为阴线，最高价高于 SMA20，且 Chaikin 大于 `CloseLevel`（默认 90）时，全部平仓。
- **空单**：下一根 K 线为阳线，最低价低于 SMA20，且 Chaikin 低于 `-CloseLevel` 时，全部平仓。
- 所有仓位同时挂载 `StopLossPoints` 与 `TakeProfitPoints` 指定的止损/止盈距离（以价格最小变动单位计）。

## 风险控制
- 止损距离：`StopLossPoints × PriceStep`（默认 70 点）。
- 止盈距离：`TakeProfitPoints × PriceStep`（默认 140 点）。
- 下单手数：由 `TradeVolume` 控制，并映射到策略的 `Volume` 属性。

## 参数说明
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | 0.1 | 市价单的交易量（按品种计量单位）。 |
| `MaLength` | 20 | 拉回过滤所用 SMA 的周期。 |
| `StopLossPoints` | 70 | 止损距离（价格步长数量）。 |
| `TakeProfitPoints` | 140 | 止盈距离（价格步长数量）。 |
| `OpenLevel` | 55 | 触发开仓的 Chaikin 绝对阈值。 |
| `CloseLevel` | 90 | 触发强制平仓的 Chaikin 绝对阈值。 |
| `ChaikinFastLength` | 3 | Chaikin 计算中快 EMA 的周期。 |
| `ChaikinSlowLength` | 10 | Chaikin 计算中慢 EMA 的周期。 |
| `CandleType` | H1 | 进行计算所使用的 K 线周期。 |

## 实现细节
- 通过 `SubscribeCandles().Bind(...)` 将 K 线与指标连接，完全遵守高层 API 的使用规范。
- Chaikin 数值在内存中通过 ADL → EMA 管道实时重建，无需调用被禁止的 `GetValue()` 接口。
- 策略缓存上一根已完成 K 线的 OHLC、SMA 与 Chaikin 数据，以复现 MQL 中对 `Close[1]`、`Low[1]`、`High[1]` 和 `iCustom(...,1)` 的引用方式。
- 止损与止盈由策略内部追踪，因为原始 EA 仅发送市价单并手动设置保护距离。
