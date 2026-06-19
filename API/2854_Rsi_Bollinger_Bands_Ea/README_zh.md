# RSI Bollinger Bands EA（StockSharp 版本）

## 概述
该策略是 MetaTrader 5 指标交易程序 “RSI Bollinger Bands EA” 的 StockSharp 高层 API 改写版本。策略在 15 分钟周期上运行，并提供两套可互斥的 RSI 触发条件：

* **触发器一**：在 M15、H1、H4 三个周期上分别使用固定的 RSI 超买/超卖水平，同时结合 M15 RSI 斜率和随机指标过滤。
* **触发器二**：对每个周期维护一段 RSI 历史序列，计算正、负方向的非对称标准差，构建自适应的上下通道。

所有触发器都要求：H4 Bollinger 带宽足够大、M15 带宽足够小以及 H4 ATR 不高于限制值；与原始 EA 一样，只能启用其中一个触发器。

## 数据需求
* 主交易周期：`M15CandleType`（默认 15 分钟）。所有入场与出场在 M15 收盘时评估。
* 确认周期：`H1CandleType`（默认 1 小时），用于 RSI 条件及统计计算。
* 高阶周期：`H4CandleType`（默认 4 小时），用于 Bollinger 带宽过滤和 ATR 过滤。

## 交易逻辑
1. **交易时段限制**
   * `EntryHour` 定义交易窗口起点，`OpenHours` 指定持续小时数。若 `OpenHours = 0`，窗口仅持续一个小时。
   * 周五当 M15 开盘小时达到 `FridayEndHour` 时停止开仓。
   * 仅在净持仓为 0 时开新仓位。

2. **波动率过滤（两个触发器通用）**
   * H4 Bollinger 带宽需大于 `BbSpreadH4MinX`（X=1 或 2）以确认高阶波动。
   * M15 Bollinger 带宽需小于 `BbSpreadM15MaxX`，表示价格正在压缩。
   * H4 ATR（换算为点数）必须低于 `AtrLimit`。

3. **触发器一 – 固定 RSI 阈值**
   * M15/H1/H4 RSI 必须低于 “Low” 阈值但高于 “Low Limit” 才能做多；做空则要求 RSI 高于 “High” 但低于 “High Limit”。
   * M15 RSI 与上一根值的差必须大于 `RDeltaM15Lim1`（做空取相反符号），用于检测动量拐点。
   * 随机指标主线需低于 `StocLoM15_1`（做多）或高于 `StocHiM15_1`（做空）。

4. **触发器二 – 自适应 RSI 通道**
   * 每个周期保存最多 `NumRsi` 个 RSI 值，分别计算均值以及正、负方向的方差。
   * 使用 `Rsi*M*Sigma2` 构建主要上下界，并利用 `Rsi*M*SigmaLim2` 设置更宽的限制区间。
   * 做多需要 RSI 值跌破主要下界但仍位于下限制之上，同时随机指标 < `StocLoM15_2`，且 RSI 斜率 > `RDeltaM15Lim2`。
   * 做空条件对称处理。

5. **下单与退出**
   * 触发后以 `Volume`（默认 0.1 手）成交的市价单入场。
   * 停损和止盈价位根据相应触发器的点数参数以及品种点值计算。
   * 策略在每根 M15 完结时检查最高价/最低价，一旦触及保护价格即通过市价平仓，并重置保护位，以模拟原 EA 的止损/止盈委托。

## 参数列表
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `Volume` | 交易手数。 | `0.1` |
| `TriggerOne` | 启用固定 RSI 触发器。 | `true` |
| `TriggerTwo` | 启用自适应 RSI 触发器（与触发器一互斥）。 | `false` |
| `BbSpreadH4Min1` | 触发器一的 H4 Bollinger 最小带宽（点）。 | `84` |
| `BbSpreadM15Max1` | 触发器一的 M15 Bollinger 最大带宽（点）。 | `64` |
| `RsiPeriod1` | 触发器一 RSI 周期。 | `10` |
| `RsiLoM15_1`, `RsiHiM15_1` | M15 RSI 阈值。 | `24`, `66` |
| `RsiLoH1_1`, `RsiHiH1_1` | H1 RSI 阈值。 | `34`, `54` |
| `RsiLoH4_1`, `RsiHiH4_1` | H4 RSI 阈值。 | `48`, `56` |
| `RsiLoLim*`, `RsiHiLim*` | 极值保护界限。 | `20–92` |
| `RDeltaM15Lim1` | M15 RSI 最小斜率（正值做多、负值做空）。 | `-3.5` |
| `StocLoM15_1`, `StocHiM15_1` | 触发器一的随机指标阈值。 | `26`, `64` |
| `BbSpreadH4Min2` | 触发器二的 H4 Bollinger 最小带宽。 | `65` |
| `BbSpreadM15Max2` | 触发器二的 M15 Bollinger 最大带宽。 | `75` |
| `RsiPeriod2` | 触发器二 RSI 周期。 | `20` |
| `NumRsi` | RSI 历史样本数量。 | `60` |
| `Rsi*M*Sigma2` | 主通道标准差倍数（M15/H1/H4）。 | `1.20 / 0.95 / 0.9` |
| `Rsi*M*SigmaLim2` | 限制通道标准差倍数。 | `1.85 / 2.55 / 2.7` |
| `RDeltaM15Lim2` | 触发器二的 M15 RSI 最小斜率。 | `-5.5` |
| `StocLoM15_2`, `StocHiM15_2` | 触发器二的随机指标阈值。 | `24`, `68` |
| `TakeProfitBuy1`, `StopLossBuy1` | 触发器一做多的止盈/止损（点）。 | `150`, `70` |
| `TakeProfitSell1`, `StopLossSell1` | 触发器一做空的止盈/止损（点）。 | `70`, `35` |
| `TakeProfitBuy2`, `StopLossBuy2` | 触发器二做多的止盈/止损。 | `140`, `35` |
| `TakeProfitSell2`, `StopLossSell2` | 触发器二做空的止盈/止损。 | `60`, `30` |
| `AtrPeriod` | H4 ATR 周期。 | `60` |
| `BollingerPeriod` | M15/H4 Bollinger 长度。 | `20` |
| `AtrLimit` | ATR 上限（点）。 | `90` |
| `EntryHour` | 交易窗口起始小时。 | `0` |
| `OpenHours` | 交易窗口持续时间（0 = 仅 1 小时）。 | `14` |
| `NumPositions` | 最大净仓位数量（本策略仅在空仓时开仓）。 | `1` |
| `FridayEndHour` | 周五停止交易的小时。 | `4` |
| `StochasticK`, `StochasticD`, `StochasticSlowing` | 随机指标参数。 | `12 / 5 / 5` |
| `M15CandleType`, `H1CandleType`, `H4CandleType` | 各周期的蜡烛数据类型。 | `15m / 1h / 4h` |

## 备注
* 原 EA 使用实际止损/止盈委托，本移植通过检查 M15 蜡烛的最高/最低价来模拟，若需要逐笔精度，可改用底层 API 下达止损单。
* 请确保数据源提供全部三个时间框，否则 RSI 队列无法形成，策略不会触发。
* 点值根据 `PriceStep` 推导，若价格步长对应 5 位或 3 位小数，则乘以 10，与 MetaTrader 习惯保持一致。
