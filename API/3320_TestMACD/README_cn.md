# Test MACD 策略

## 概览
**Test MACD 策略** 是 MetaTrader `TestMACD` 专家顾问在 StockSharp 高级 API 中的完整复刻版。策略使用移动平均收敛散度（MACD）指标识别动量变化，在每根收盘的 K 线中，当 MACD 主线与信号线发生交叉时执行交易。策略通过 `CandleType` 参数控制所订阅的单一标的及时间框架。

## 交易逻辑
1. 根据 `CandleType` 订阅蜡烛数据，并按可配置的快、慢、信号周期计算 MACD 指标。
2. 在每根收盘 K 线上监控 MACD 差值（`MACD - Signal`）。
3. 当差值从非正变为正值时触发**做多入场**，即 MACD 主线向上穿越信号线。在开多仓之前，会先平掉所有空头持仓。
4. 当差值从非负变为负值时触发**做空入场**，即 MACD 主线向下穿越信号线。在开空仓之前，会先平掉所有多头持仓。
5. 所有订单均以市场价发送，使用 `TradeVolume` 参数设定的固定手数。
6. 为每次入场自动设置以价格步长表示的止损和止盈距离，重现原始专家顾问的点值风险控制。

## 风险管理
- 止损和止盈距离继承自 MetaTrader 输入，单位为价格步长。当标的缺少 `PriceStep` 信息时，策略会退化为使用 `MinPriceStep` 或 `1` 的绝对价格距离。
- 通过 `StartProtection` 在策略启动时一次性建立保护设置，之后的每笔交易都会自动沿用，无需重复配置。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `FastPeriod` | MACD 快速 EMA 长度。 | `12` |
| `SlowPeriod` | MACD 慢速 EMA 长度。 | `24` |
| `SignalPeriod` | MACD 信号线 EMA 长度。 | `9` |
| `StopLossPoints` | 以价格步长表示的止损距离。 | `90` |
| `TakeProfitPoints` | 以价格步长表示的止盈距离。 | `110` |
| `TradeVolume` | 每次下单的固定手数。 | `1` |
| `CandleType` | 策略订阅的蜡烛数据类型与周期。 | `30 分钟时间框架` |

## 使用提示
- 启动前请先将策略绑定到具体证券，以便读取 `PriceStep` 和 `MinPriceStep` 信息。
- 需要确保所选 `CandleType` 拥有实时或历史数据，否则 MACD 指标无法形成，策略也不会交易。
- 策略会记录每次交叉事件，便于在回测时追踪入场与出场原因。

## 转换细节
- 原有 MetaTrader 类 `CSignalMACD`、`CTrailingNone` 与 `CMoneyFixedLot` 分别由 StockSharp 的指标绑定机制与 `StartProtection` 功能替代。
- `ExtStateMACD` 中用于检测交叉的逻辑在转换后通过连续收盘 K 线的 MACD 差值符号变化来实现。
- 资金管理简化为固定手数参数，与 `CMoneyFixedLot` 在关闭百分比调节后的行为最为接近。
