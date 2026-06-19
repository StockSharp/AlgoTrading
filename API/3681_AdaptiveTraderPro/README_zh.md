# AdaptiveTrader Pro 策略

## 概述
AdaptiveTrader Pro 是由 MetaTrader 5 智能交易系统 *AdaptiveTrader_Pro_Final_EA.mq5* 转换而来的多时间框趋势策略。它结合 RSI、ATR 以及双重均线，在趋势方向上寻找交易机会并执行资金管理控制。

该策略使用可配置的主时间框（默认 5 分钟），并利用更高时间框（默认 1 小时）的移动平均线确认趋势方向。入场基于 RSI 超买/超卖信号，同时要求价格位于两条均线的同一侧。

## 交易规则
- **做多**：当 RSI 低于 30，且蜡烛收盘价高于主时间框 SMA 与高时间框 SMA。
- **做空**：当 RSI 高于 70，且蜡烛收盘价低于两条 SMA。
- **单向持仓**：同一时间只持有一个方向的仓位，反向信号出现前会先平掉已有仓位。

## 风险与仓位管理
- **仓位大小**：根据账户权益、风险百分比以及 ATR 止损距离自动计算下单数量。
- **止损处理**：基于 ATR 的跟踪止损跟随价格移动，并在价格按设定倍数向有利方向发展后将止损上移至保本位。
- **分批止盈**：在达到第一个 ATR 倍数目标时按设定比例部分止盈，剩余仓位由跟踪止损管理。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `MaxRiskPercent` | 每笔交易占用的账户风险百分比。 | `0.2` |
| `RsiPeriod` | 主时间框的 RSI 周期。 | `14` |
| `AtrPeriod` | 主时间框的 ATR 周期。 | `14` |
| `AtrMultiplier` | 初始止损的 ATR 倍数。 | `1.5` |
| `TrailingStopMultiplier` | 跟踪止损使用的 ATR 倍数。 | `1.0` |
| `TrailingTakeProfitMultiplier` | 首次止盈目标的 ATR 倍数。 | `2.0` |
| `TrendPeriod` | 主时间框 SMA 周期。 | `20` |
| `HigherTrendPeriod` | 高时间框 SMA 周期。 | `50` |
| `BreakEvenMultiplier` | 触发保本止损的 ATR 倍数。 | `1.5` |
| `PartialCloseFraction` | 首次止盈时平掉的仓位比例。 | `0.5` |
| `MaxSpreadPoints` | 允许开仓的最大点差（按最小价格跳动计）。 | `20` |
| `CandleType` | 用于分析的主时间框 K 线类型。 | `5 分钟 K 线` |
| `HigherCandleType` | 用于确认趋势的高时间框 K 线类型。 | `1 小时 K 线` |

## 说明
- 策略使用 StockSharp 高级 API，通过蜡烛订阅与指标绑定执行逻辑。
- 通过最佳买卖报价监控点差；若点差超过阈值则暂停开仓。
- 根据要求暂不提供 Python 版本。
