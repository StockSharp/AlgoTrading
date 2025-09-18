# MarsiEaStrategy

## 概览

`MarsiEaStrategy` 在 StockSharp 高级 API 中复刻了 MetaTrader 上的 MARSIEA 专家顾问。策略以简单移动平均线（SMA）配合相对强弱指标（RSI）判定方向，并且在任意时刻仅持有一笔仓位。止损与止盈均以点（pip）为单位，与原版保持一致；下单手数按账户权益和风险百分比动态计算。

## 交易逻辑

1. **数据准备**
   - 在所选 K 线序列上计算可配置周期的 SMA。
   - 使用相同的 K 线计算可配置周期的 RSI。
   - K 线类型通过 `CandleType` 参数设置，默认使用 1 分钟 K 线。

2. **入场条件**
   - 只有在两个指标都完成计算且当前没有持仓时才会评估信号。
   - **做多：** 收盘价位于 SMA 之上，同时 RSI 低于超卖阈值。
   - **做空：** 收盘价位于 SMA 之下，同时 RSI 高于超买阈值。
   - 为保持与原版一致，策略在任何仓位未平仓时不会再次开仓。

3. **离场条件**
   - 每次开仓后立即登记以点数定义的固定止损和止盈。
   - 不设额外离场规则，保护单会负责平仓。

## 风险控制与仓位管理

- `RiskPercent` 决定每笔交易愿意承担的账户权益百分比。
- Pip 数值根据 `Security.PriceStep`、`Security.StepPrice` 以及品种的小数位计算，复刻 MQL 中 `_Digits` 的判断方式。
- 手数会按照 `Security.VolumeStep` 四舍五入，并遵守 `Security.VolumeMin` 指定的最小交易量。
- 若因缺少品种信息或止损距离为零而无法完成风险计算，策略会退回到 `Volume` 属性（默认 1 手）。

## 参数说明

| 参数 | 说明 |
|------|------|
| `CandleType` | 指标使用的 K 线序列。 |
| `MaPeriod` | SMA 的计算周期。 |
| `RsiPeriod` | RSI 的计算周期。 |
| `RsiOverbought` | 触发做空的 RSI 超买阈值。 |
| `RsiOversold` | 触发做多的 RSI 超卖阈值。 |
| `RiskPercent` | 每笔交易承担的权益百分比。 |
| `StopLossPips` | 以点数表示的止损距离。 |
| `TakeProfitPips` | 以点数表示的止盈距离。 |

## 转换说明

- 原始 EA 在买卖价上开仓；由于高级 API 不提供逐笔报价，这个移植版本使用 K 线收盘价作为入场参考。
- Pip 计算遵循原版逻辑：当品种保留 5 或 3 位小数时，pip 等于价格步长的 10 倍。
- 调用 `StartProtection()` 以便框架自动为持仓附加止损/止盈订单。
- 策略完全保留“持仓期间不再开新仓”的原始行为。
