# Freeman 策略
[English](README.md) | [Русский](README_ru.md)

Freeman 是一套日内趋势跟随策略，通过多层动量过滤逐步建立仓位。交易级别上使用两组 RSI “教师”配合不同周期的移动平均线，高一级别的移动平均线用于确认趋势。策略利用 ATR 比例的止损/止盈以及按点数计算的追踪止损来控制风险。

## 策略概览

- 通过 `CandleType` 参数选择主交易周期（默认 15 分钟）。
- `FilterCandleType` 提供更高时间框架的数据（默认 1 小时）用于趋势过滤。
- 两个 RSI 教师模块比较当前与前一根的数值，同时检查移动平均线的斜率来生成多空信号。
- 当行情继续朝有利方向运行时允许加仓，若上一次同向平仓亏损，可按 `LockCoefficient` 放大新订单的数量。

## 交易逻辑

### 多头条件

1. 启用趋势过滤时，需要更高周期的移动平均线向上倾斜。
2. RSI 教师 #1 触发条件：
   - RSI #1 在上一根低于 `RsiSellLevel`，当前向上拐头；
   - 快速移动平均线上升；
   - 小时级 RSI (14) 低于 `RsiBuyLevel`，避免高位追涨。
3. RSI 教师 #2 触发条件：
   - RSI #2 在上一根低于 `RsiSellLevel2`，当前向上；
   - 慢速移动平均线上升；
   - 小时级 RSI 低于 `RsiBuyLevel2`。
4. 至少有一个教师满足条件并且趋势过滤通过时开多。
5. 再次加多需价格相对上一笔成交偏离超过 `DistancePips`（换算成实际价格步长）。如果上一笔多头平仓亏损，本次下单数量乘以 `LockCoefficient`。

### 空头条件

- 逻辑与多头相反：
  - 启用过滤时，更高周期移动平均线必须向下；
  - 教师 #1 要求 RSI #1 高于 `RsiBuyLevel` 后转跌、快速均线走弱、小时级 RSI 高于 `RsiSellLevel`；
  - 教师 #2 要求 RSI #2 高于 `RsiBuyLevel2` 后转跌、慢速均线走弱、小时级 RSI 高于 `RsiSellLevel2`；
  - 加仓距离与锁仓系数规则相同。

## 仓位管理

- 每次开仓都按当前 ATR * `StopLossAtrFactor`/`TakeProfitAtrFactor` 重新计算止损和止盈。
- 当价格突破 `TrailingStopPips + TrailingStepPips` 后启动追踪止损，将止损维持在距离最新收盘 `TrailingStopPips` 的位置。
- 当蜡烛的最高价/最低价触及止损或止盈水平时，直接用市价单平仓。
- `PositionsMaximum` 限制多空成交总数，设置为 0 表示不限次数。

## 时间过滤

- 通过 `TradeOnFriday` 可以禁止周五交易。
- `StartHour` 与 `EndHour` 用于设定交易时间窗口（交易所时间），为 0 表示全天可交易。

## 参数列表

| 名称 | 说明 |
| --- | --- |
| `CandleType` | 主交易时间框架。 |
| `FilterCandleType` | 趋势过滤所用的高时间框架（默认 1 小时）。 |
| `FirstMaPeriod` / `SecondMaPeriod` | 快速与慢速移动平均线周期。 |
| `FilterMaPeriod` | 高时间框架移动平均线长度。 |
| `MaType` | 移动平均线类型（SMA/EMA/SMMA/WMA）。 |
| `RsiFirstPeriod` / `RsiSecondPeriod` | 两个 RSI 教师的周期。 |
| `RsiSellLevel`, `RsiBuyLevel`, `RsiSellLevel2`, `RsiBuyLevel2` | RSI 触发阈值。 |
| `UseRsiTeacher1`, `UseRsiTeacher2`, `UseTrendFilter` | 各功能模块的开关。 |
| `StopLossAtrFactor`, `TakeProfitAtrFactor` | ATR 止损/止盈倍数。 |
| `TrailingStopPips`, `TrailingStepPips` | 追踪止损的距离和步长（以点计算）。 |
| `PositionsMaximum` | 限制加仓次数，0 表示无限制。 |
| `DistancePips` | 允许加仓的最小点数距离。 |
| `TradeOnFriday` | 是否允许周五入场。 |
| `StartHour`, `EndHour` | 可选的交易时间窗。 |
| `LockCoefficient` | 亏损后加仓的数量倍数。 |
| `SignalShift` | 指标读取的偏移量（0 代表当前已完成的蜡烛）。 |

## 实现说明

- 迁移到 StockSharp 后仅在蜡烛收盘时计算信号，对应 MT5 中启用 Bars Control 的行为；不支持逐笔信号。
- 所有以点表示的距离都会乘以标的的 `PriceStep` 转换成价格单位。
- 止损/止盈/追踪逻辑通过高层 API 使用市价平仓，而非像 MT5 那样逐笔修改仓位。
- 策略记录多空方向的累计持仓量，方向完全平仓后会重置亏损标记，从而再现原始的锁单逻辑。

在真实市场使用前，请充分回测并配置适合自身的风险管理方案。
