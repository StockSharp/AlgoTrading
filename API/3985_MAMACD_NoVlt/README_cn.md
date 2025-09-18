# MAMACD 无波动策略

## 概述
MAMACD 无波动策略是 MetaTrader 4 专家顾问 `MAMACD_novlt.mq4` 的直接移植。策略把三条分别基于最低价和收盘价的均线与 MACD 动量过滤器结合起来：当快速 EMA 跌破（做多）或升破（做空）两条基于最低价的 LWMA 时，先进入准备状态，随后只有在 MACD 主线确认动量方向后才执行进场。

## 指标
- **快速 EMA** (`FastEmaPeriod`)，基于收盘价。
- **LWMA 1** (`FirstLowWmaPeriod`)，基于最低价。
- **LWMA 2** (`SecondLowWmaPeriod`)，基于最低价。
- **MACD 主线**，快速周期为 `FastSignalEmaPeriod`，慢速周期为 `SlowEmaPeriod`。

所有指标都使用 `CandleType` 指定的时间框架（默认：5 分钟 K 线）。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `FirstLowWmaPeriod` | 第一条基于最低价的 LWMA 周期。 | 85 |
| `SecondLowWmaPeriod` | 第二条基于最低价的 LWMA 周期。 | 75 |
| `FastEmaPeriod` | 基于收盘价的快速 EMA 周期。 | 5 |
| `SlowEmaPeriod` | MACD 计算的慢速周期。 | 26 |
| `FastSignalEmaPeriod` | MACD 计算的快速周期。 | 15 |
| `StopLossPoints` | 止损距离（价格步长，0 表示不启用）。 | 15 |
| `TakeProfitPoints` | 止盈距离（价格步长，0 表示不启用）。 | 15 |
| `TradeVolume` | 每次进场的下单手数。 | 0.1 |
| `CandleType` | 所有指标使用的 K 线序列。 | 5 分钟 |

## 交易规则
1. **准备做多**：快速 EMA 位于两条 LWMA 之下。
2. **准备做空**：快速 EMA 位于两条 LWMA 之上。
3. **开多仓**：
   - 快速 EMA 再次站上两条 LWMA；
   - 之前已经完成多头准备；
   - MACD 主线为正或高于上一值；
   - 当前净仓位不是多头。
4. **开空仓**：
   - 快速 EMA 再次跌破两条 LWMA；
   - 之前已经完成空头准备；
   - MACD 主线为负或低于上一值；
   - 当前净仓位不是空头。
5. **风险控制**：通过策略的保护模块自动应用可选的止损与止盈。当参数为 0 时，对应的保护不会启动。

策略不包含额外的离场条件，仓位的退出依赖设定的止损/止盈或手动操作。

## 说明
- MACD 的确认逻辑与原始 MQL 版本一致：做多时主线必须高于 0 或者比上一笔更高；做空时主线必须低于 0 或者比上一笔更低。
- 两条 LWMA 使用最低价计算，与原始顾问保持一致。
- `TradeVolume` 参数直接决定每次市价单的交易数量，从而复刻 MQL 版本的下单方式。
