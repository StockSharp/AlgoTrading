# 玛格纳掠夺者铜版策略

该策略复现原始 MQL 专家中的“彩虹”均线系统。
它使用十一条指数移动平均线，并结合 MACD 与 ADX 过滤器。

## 工作原理

- 计算 EMA(2)、EMA(3)、EMA(5)、EMA(8)、EMA(13)、EMA(21)、EMA(34)、EMA(55)、EMA(89)、EMA(144) 和 EMA(233) 的收盘价。
- 计算 MACD（快线、慢线、信号线），使用信号线判断方向。
- 计算 ADX 以评估趋势强度。
- **买入** 条件：
  - MACD 信号线大于零；
  - 所有 EMA 严格递增（快线在慢线之上）；
  - ADX 大于阈值。
- **卖出** 条件：
  - MACD 信号线小于零；
  - 所有 EMA 严格递减；
  - ADX 大于阈值。

出现相反信号时，仓位反向。

## 参数

| 名称 | 说明 |
| --- | --- |
| `FastMacd` | MACD 快速 EMA 周期 |
| `SlowMacd` | MACD 慢速 EMA 周期 |
| `SignalPeriod` | MACD 信号线周期 |
| `AdxPeriod` | ADX 指标周期 |
| `AdxThreshold` | 进行交易所需的最小 ADX 值 |
| `CandleType` | 计算所使用的K线周期 |

## 备注

- 策略通过 `BuyMarket` 和 `SellMarket` 下达市价单。
- 同一时间仅持有一个方向的仓位，出现反向信号时将反向开仓。
- 未实现原策略中的可选马丁格尔逻辑。
