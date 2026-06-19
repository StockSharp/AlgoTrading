# Good Gbbi 策略

该策略在每天指定的时间根据历史开盘价差异开仓一次。

## 逻辑

* 默认使用小时K线。
* 在 `TradeTime` 小时，比较 `T1` 和 `T2` 根K线前的开盘价。
* 如果较早的开盘价高于最近的开盘价 `DeltaShort` 点，则开空仓。
* 如果最近的开盘价高于较早的开盘价 `DeltaLong` 点，则开多仓。
* 每天仅允许一笔交易，当时间超过 `TradeTime` 后才允许再次交易。
* 每笔持仓都有独立的止盈和止损，并且在超过 `MaxOpenTime` 小时后会被强制平仓。

## 参数

| 参数 | 说明 |
|------|------|
| `TakeProfitLong` | 多头仓位的止盈点数。 |
| `StopLossLong` | 多头仓位的止损点数。 |
| `TakeProfitShort` | 空头仓位的止盈点数。 |
| `StopLossShort` | 空头仓位的止损点数。 |
| `TradeTime` | 检查入场条件的小时。 |
| `T1` | 第一个开盘价回溯的K线数量。 |
| `T2` | 第二个开盘价回溯的K线数量。 |
| `DeltaLong` | 触发多头入场所需的点数差。 |
| `DeltaShort` | 触发空头入场所需的点数差。 |
| `MaxOpenTime` | 持仓的最长持续时间（小时），0 表示不限制。 |
| `CandleType` | 处理的K线类型。 |

## 说明

该策略来源于 MetaTrader 专家顾问 *GoodG@bi*。本版本使用 StockSharp 高级 API，仅处理已完成的K线。请确保品种的 `PriceStep` 设置正确以便解析点值。
