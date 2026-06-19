# VIDYA N Bars Borders Martingale

## 概述
原始的 MetaTrader 策略将 “VIDYA N Bars Borders” 通道与马丁格尔资金管理结合。StockSharp 版本保留核心思想：当价格跌破自适应下轨时买入，当价格突破上轨时卖出。通道中线由自适应移动平均生成，宽度由平均真实波幅（ATR）定义。马丁格尔模块在出现亏损后放大下一笔交易，同时检查单笔和总持仓上限。

## 交易逻辑
1. 订阅所选周期的蜡烛图数据。
2. 计算 `KaufmanAdaptiveMovingAverage`（作为 VIDYA 的替代）并构建 ATR 通道。
3. 若收盘价跌破下轨，则开多或反手做多；若启用 `Reverse` 参数，则执行相反方向。
4. 若收盘价突破上轨，则开空或反手做空；`Reverse` 为真时转为做多。
5. 约束相邻两次入场之间的最小价格间距，避免在同一区域重复进场。
6. 当浮动收益达到指定的金额目标时，立即平掉所有仓位。
7. 每次平仓后，如果上一笔交易亏损，则将下一笔基础手数乘以马丁格尔系数；若盈利则恢复到基础手数。最终手数会按照交易品种的步长和限额自动调整。

## 参数
| 名称 | 说明 |
| --- | --- |
| `Candle Type` | 交易所使用的蜡烛数据类型。 |
| `CMO Period` | 自适应均线效率比窗口。 |
| `EMA Period` | 自适应均线的平滑周期。 |
| `ATR Period` | ATR 通道的计算周期。 |
| `Profit Target` | 达到该金额时立即平仓。 |
| `Increase Ratio` | 亏损后下一笔手数的放大倍数。 |
| `Max Position Volume` | 单笔头寸体量上限。 |
| `Max Total Volume` | 策略允许的总敞口上限。 |
| `Max Positions` | 同时持仓数量上限（此移植版本只维护一个净头寸）。 |
| `Minimum Step` | 连续两次入场的最小点数间隔。 |
| `Base Volume` | 未放大之前的基础手数。 |
| `Reverse Signals` | 反向执行买卖信号。 |

## 实现说明
- StockSharp 暂无原生 VIDYA 指标，因此使用 `KaufmanAdaptiveMovingAverage` 近似其自适应特性，可通过参数调整响应速度。
- 策略仅维护一个净头寸。MQL 版本可以排队多个挂单；在 StockSharp 中，每次信号要么开新仓，要么反向平仓。马丁格尔调整作用于下一次入场的手数。
- 最小入场间距和手数调整依赖品种元数据（`PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume`），请在连接交易所/历史数据时提供这些信息。
- 盈亏跟踪基于策略 `PnL` 和最新收盘价，适用于回测。实盘运行时请连接会实时更新收益的投资组合。

## 文件
- `CS/VidyaNBarsBordersMartingaleStrategy.cs` — 策略的 C# 实现。
