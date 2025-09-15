# 20/200 Expert v4.2 AntS 策略

该策略每天最多在用户设定的时间开仓一次。它比较两个过去K线的开盘价（T1 和 T2）。如果较早的K线比较晚的高出 DeltaShort 点，则做空；如果较晚的K线高出 DeltaLong 点，则做多。

仓位数量可以固定，也可以根据账户余额自动计算。如果余额比上一笔交易减少，手数会乘以 BigLotSize。

每笔交易都有自己的止盈和止损点数，并且 MaxOpenTime 参数限制了持仓的最长时间（小时）。

## 参数

- `CandleType` – 使用的K线周期（默认1小时）。
- `TradeHour` – 检查入场条件的小时。
- `T1`, `T2` – 用于比较开盘价的K线偏移。
- `DeltaLong`, `DeltaShort` – 触发信号的最小开盘价差（点）。
- `TakeProfitLong`, `StopLossLong` – 多单止盈/止损（点）。
- `TakeProfitShort`, `StopLossShort` – 空单止盈/止损（点）。
- `Lot` – 基础交易手数。
- `AutoLot` – 是否启用自动手数计算。
- `BigLotSize` – 亏损后手数的放大倍数。
- `MaxOpenTime` – 持仓最长时间（小时）。
