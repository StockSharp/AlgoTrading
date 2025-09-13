# LotScalp 策略

该策略每天在指定的小时根据过去K线开盘价之间的差值开仓一次。

## 工作原理

1. **等待交易时间**：策略监控K线的开盘时间，一旦当前小时大于 `TradeTime`，下一次达到该小时即可允许交易。
2. **信号生成**：
   - 当当前小时等于 `TradeTime` 时，比较 `t1` 根之前的开盘价与 `t2` 根之前的开盘价。
   - 如果 `Open[t1] - Open[t2]` 大于 `DeltaShort` 点，则开空单。
   - 如果 `Open[t2] - Open[t1]` 大于 `DeltaLong` 点，则开多单。
3. **持仓管理**：
   - 多单在价格上升 `TakeProfitLong` 点或下跌 `StopLossLong` 点时平仓。
   - 空单在价格下跌 `TakeProfitShort` 点或上升 `StopLossShort` 点时平仓。
   - 若持仓时间超过 `MaxOpenTime` 小时，也会强制平仓。

策略使用固定交易量，每天仅交易一次。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `CandleType` | 使用的K线类型。 |
| `Volume` | 下单手数。 |
| `TakeProfitLong` | 多单止盈点数。 |
| `StopLossLong` | 多单止损点数。 |
| `TakeProfitShort` | 空单止盈点数。 |
| `StopLossShort` | 空单止损点数。 |
| `TradeTime` | 评估信号的小时。 |
| `T1` | 第一个参考开盘价的回溯K线数。 |
| `T2` | 第二个参考开盘价的回溯K线数。 |
| `DeltaLong` | 触发多单所需的点差。 |
| `DeltaShort` | 触发空单所需的点差。 |
| `MaxOpenTime` | 最大持仓时间（小时）。 |

## 备注

- 仅处理已完成的K线。
- 点数阈值通过合约的最小变动价位转换为实际价格。
- 策略未使用任何额外指标。
