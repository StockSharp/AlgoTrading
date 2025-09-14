# MPM动量策略

该策略是对原始MQL专家顾问`mpm-1_8.mq4`的简化转换。策略等待一系列同方向的K线，
当满足条件时在同方向开仓。平均真实波动范围(ATR)用于评估K线大小并跟踪止损。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `ProgressiveCandles` | 触发交易所需的连续K线数量。 |
| `ProgressiveSize` | 相对于ATR的最小K线实体大小。 |
| `StopRatio` | 用于跟踪止损的ATR比例。 |
| `AtrPeriod` | ATR指标的周期。 |
| `CandleType` | 策略使用的K线类型。 |
| `ProfitPerLot` | 每手的盈利目标。 |
| `BreakEvenPerLot` | 达到保本所需的盈利。 |
| `LossPerLot` | 每手可接受的最大亏损。 |

## 逻辑

1. 在每根收盘K线上比较其实体大小与ATR。
2. 当K线实体超过`ProgressiveSize`阈值时，记录多头或空头计数。
3. 当同方向连续出现`ProgressiveCandles`根K线后，发送市价单。
4. 止损价格按`StopRatio`×ATR进行跟踪。
5. 当触及止损或达到盈利/亏损目标时平仓。
