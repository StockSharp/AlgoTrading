# Exp X2MA Candle MM Recovery 策略

## 概览
本策略是 MetaTrader 专家顾问 **Exp_X2MACandle_MMRec** 的 C# 版本。它通过原始 X2MA 指标得到的双重平滑蜡烛颜色来管理仓位，当颜色发生变化时开仓或平仓。同时，策略记录最近的交易结果，在连续亏损时自动降低下单手数，实现一个简化的资金管理机制。

算法只处理已经收盘的蜡烛。订阅用户设定的时间框架，对每根蜡烛的四个价格（开高低收）连续应用两种可配置的移动平均。随后按照平滑后的开收价计算蜡烛颜色：开盘价 < 收盘价 → `2`（看多），开盘价 > 收盘价 → `0`（看空），否则为 `1`（中性）。默认情况下信号会在向前两根完成蜡烛上评估，避免在当前蜡烛尚未完全形成时重复下单。

## 指标细节
1. 蜡烛的四个价格序列都会经过两级移动平均；每一级的方式和周期可以单独设置。
2. 平滑方式与 StockSharp 指标的映射关系：
   - `Simple` → `SimpleMovingAverage`
   - `Exponential` → `ExponentialMovingAverage`
   - `Smoothed` → `SmoothedMovingAverage`
   - `Weighted` → `WeightedMovingAverage`
   - `Jurik` → `JurikMovingAverage`（如果库暴露 Phase 属性，则会写入 MQL 中的相位参数）。
3. 当平滑后的开收价绝对差值小于 `GapPoints * StepPrice` 时，开盘价会被替换为上一根蜡烛的收盘价，使得身体长度为零。
4. 根据信号条数 `SignalBar`（默认 1）向后取颜色序列：如果两根前的颜色为 `2` 且前一根不为 `2`，策略认为出现做多信号；颜色为 `0` 时产生做空信号，同时也可以选择性地触发相反方向的平仓。

## 资金管理
- 原版顾问通过 MetaTrader 历史成交信息来判断是否需要减小手数。StockSharp 无法访问同样的接口，因此移植版在内部维护一条固定长度的队列，记录最近 `HistoryDepth` 笔已关闭交易的盈亏情况。
- 当队列中的亏损数量达到 `LossTrigger` 时，下一笔交易的数量切换为 `ReducedVolume`，否则使用 `NormalVolume`。
- 平仓结果通过触发信号时蜡烛的收盘价进行估算。原策略中的止损/止盈指令没有自动迁移，如需保护仓位，可使用 StockSharp 的 `StartProtection` 或其他风控组件。

## 参数说明
| 参数 | 说明 |
|------|------|
| `CandleType` | 进行平滑和交易的蜡烛时间框架。 |
| `FirstMethod` / `FirstLength` / `FirstPhase` | 第一层移动平均的方式、周期及 Jurik 相位。 |
| `SecondMethod` / `SecondLength` / `SecondPhase` | 第二层移动平均的方式、周期及相位。 |
| `GapPoints` | 以最小价格变动为单位的身体压扁阈值。 |
| `SignalBar` | 读取颜色时向后偏移的蜡烛数量。 |
| `AllowLongEntry` / `AllowShortEntry` | 是否允许开多/开空。 |
| `AllowLongExit` / `AllowShortExit` | 是否允许平多/平空。 |
| `NormalVolume` | 正常情况下的下单数量。 |
| `ReducedVolume` | 达到亏损阈值后使用的下单数量。 |
| `HistoryDepth` | 参与统计的最近交易数量，设为 0 可关闭该机制。 |
| `LossTrigger` | 触发减仓的亏损笔数，设为 0 表示始终使用正常手数。 |

## 使用提示
- 策略只针对一个证券运行，并在每根已完成蜡烛时执行逻辑，避免在同一信号上多次下单。
- 如果希望保留历史统计但不降低手数，可把 `ReducedVolume` 设置为与 `NormalVolume` 相同，或者把 `LossTrigger` 设为 0。
- 由于盈亏判断基于蜡烛收盘价，实际平台上的滑点或部分成交可能导致结果与 MetaTrader 有轻微差异，请根据实盘情况调整参数。
- 需要止损或止盈时，请结合 StockSharp 的风控模块添加相应设置。
