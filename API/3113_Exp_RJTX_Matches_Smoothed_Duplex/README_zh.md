# Exp RJTX Matches Smoothed Duplex

## 概述
本策略复刻 MetaTrader 5 专家顾问 `Exp_RJTX_Matches_Smoothed_Duplex.mq5`。它包含两个独立的 RJTX 模块，在各自的时间框架上对开盘价和收盘价的平滑序列进行分析。每根完成的 K 线都会根据“当前平滑收盘价是否高于 `Period` 根之前的平滑开盘价”被判定为多头或空头火柴。多头火柴驱动多头模块，空头火柴由空头模块处理。

## 信号生成
1. **平滑处理**：两个模块都将开盘价与收盘价送入所选的平滑算法。开盘与收盘使用独立的指标实例，避免内部缓冲区互相干扰。
2. **比较步骤**：当历史数据足够时，将当前平滑收盘价与 `Period` 根之前存储的平滑开盘价进行比较。
3. **火柴判定**：若收盘价更高，当前 K 线记为多头火柴，否则为空头火柴。信号在 `SignalBar` 根已收盘的 K 线之后才会被执行，与 MT5 中按偏移读取缓冲区的行为一致。

## 仓位管理
- **多头模块**：在多头火柴进入评估窗口后开多（必要时可平掉空头），出现空头火柴且允许离场时平掉多头。
- **空头模块**：与多头模块相反，空头火柴开空（可选择先平掉多头），多头火柴则平空。
- StockSharp 策略使用净头寸模式，无法像 MT5 那样同时持有多空两张独立持仓。因此在开仓前会先关闭相反方向的仓位。若不希望自动平仓，可关闭对应的 `Allow ... Close` 选项。

## 风险控制
止损和止盈以价格步长表示（`PriceStep × points`）。每根收盘的 K 线都会检查其最高价/最低价是否触及有效的止损或止盈水平，一旦触发立即平仓，从而在不依赖经纪商保护单的情况下模拟 MT5 的风险控制。

## 参数
| 分组 | 参数 | 默认值 | 说明 |
| --- | --- | --- | --- |
| Long | `LongCandleType` | H4 | 多头 RJTX 模块使用的时间框架。 |
| Long | `LongVolume` | 0.1 | 多头信号执行时的下单量。 |
| Long | `LongAllowOpen` | `true` | 是否允许开多。 |
| Long | `LongAllowClose` | `true` | 是否允许根据空头火柴平掉多头。 |
| Long | `LongStopLossPoints` | 1000 | 多头止损距离（价格步长，0 表示禁用）。 |
| Long | `LongTakeProfitPoints` | 2000 | 多头止盈距离（价格步长，0 表示禁用）。 |
| Long | `LongSignalBar` | 1 | 读取 RJTX 缓冲区时的柱子偏移（`0` 表示当前收盘柱）。 |
| Long | `LongPeriod` | 10 | 当前平滑收盘价与历史平滑开盘价之间的柱数。 |
| Long | `LongMethod` | `Sma` | 多头模块使用的平滑算法（`Sma`、`Ema`、`Smma`、`Lwma`、`Jjma`、`Jurx`、`Parma`、`T3`、`Vidya`、`Ama`）。 |
| Long | `LongLength` | 12 | 平滑滤波器的长度。 |
| Long | `LongPhase` | 15 | Jurik 类平滑的相位参数（用于兼容原策略）。 |
| Short | `ShortCandleType` | H4 | 空头 RJTX 模块使用的时间框架。 |
| Short | `ShortVolume` | 0.1 | 空头信号执行时的下单量。 |
| Short | `ShortAllowOpen` | `true` | 是否允许开空。 |
| Short | `ShortAllowClose` | `true` | 是否允许根据多头火柴平掉空头。 |
| Short | `ShortStopLossPoints` | 1000 | 空头止损距离（价格步长，0 表示禁用）。 |
| Short | `ShortTakeProfitPoints` | 2000 | 空头止盈距离（价格步长，0 表示禁用）。 |
| Short | `ShortSignalBar` | 1 | 读取 RJTX 缓冲区时的柱子偏移（空头模块）。 |
| Short | `ShortPeriod` | 10 | 当前平滑收盘价与历史平滑开盘价之间的柱数。 |
| Short | `ShortMethod` | `Sma` | 空头模块使用的平滑算法。 |
| Short | `ShortLength` | 12 | 空头模块的平滑滤波长度。 |
| Short | `ShortPhase` | 15 | 空头模块中 Jurik 类平滑的相位参数。 |

## 说明
- `Jjma` 对应 Jurik Moving Average；`Jurx`、`Parma`、`Vidya` 分别用 Zero-Lag EMA、Arnaud Legoux MA、EMA 进行近似，因为 StockSharp 暂无完全一致的 SmoothAlgorithms 过滤器。
- 止损/止盈检查基于 K 线的最高价和最低价，无法捕捉到更细粒度的盘中尖峰。
- 所有信号都在收盘后处理，遵循原版 `IsNewBar` 的逻辑。
