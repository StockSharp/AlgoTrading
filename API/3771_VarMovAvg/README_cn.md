# VarMovAvg 策略

## 概述
VarMovAvg 策略移植自 MetaTrader 4 专家顾问 `VarMovAvg_v0011`。策略通过自适应的可变均线（Variable Moving Average，VMA）来评估趋势，在出现两阶段的回踩形态（原程序称为 Bar A 与 Bar B）时执行反向建仓。当持有仓位时，系统使用移动平均线拖尾止损来保护盈利，并在出现相反方向的 Bar A/Bar B 序列时立即反手。

## 交易逻辑
1. **自适应 VMA**：自定义 `VariableMovingAverage` 指标复刻 MT4 版本的计算方式。
   - 效率比（Efficiency Ratio）比较当前收盘价与 `AmaPeriod` 根 K 之前的收盘价，再除以区间内的绝对价差之和。
   - 平滑系数在快慢周期之间插值，并按照 `SmoothingPower`（原参数 `G`）进行幂次放大。
2. **信号判定（Bar A / Bar B）**：多空各自维护独立的状态机。
   - *Bar A*：价格相对于 VMA 至少偏离 `SignalPipsBarA` 个点（pips）。
   - *Bar B*：价格继续在同方向延伸 `SignalPipsBarB` 个点，并记录极值。
   - *入场*：当收盘价回到 `SignalPipsTrade ± EntryPipsDiff` 所定义的入场带，策略发送市价单建仓或反手。
3. **拖尾止损与反手**：持仓期间对多单使用低点均线、对空单使用高点均线，并结合 `StopMaShift` 与 `StopPipsDiff` 偏移。
   - 当蜡烛触及止损线即平仓。
   - 若在持仓状态下检测到相反方向的 Bar A/Bar B，策略将按 `|Position| + Volume` 的数量一次性发送反手市价单，与原 EA 行为保持一致。

## 参数对照
| 参数 | 说明 | MT4 来源 |
|------|------|----------|
| `AmaPeriod` | VMA 的窗口长度。 | `prm.vma.periodAMA` |
| `FastPeriod` | VMA 内部的快速平滑周期。 | `prm.vma.nfast` |
| `SlowPeriod` | VMA 内部的慢速平滑周期。 | `prm.vma.nslow` |
| `SmoothingPower` | 自适应系数的幂次（原始 `G`）。 | `prm.vma.G` |
| `SignalPipsBarA` | Bar A 对 VMA 的最小偏离。 | `prm.sig.pipsBarA` |
| `SignalPipsBarB` | Bar B 额外需要的偏移。 | `prm.sig.pipsBarB` |
| `SignalPipsTrade` | 从 Bar B 极值到入场线的偏移。 | `prm.sig.pipsTrade` |
| `EntryPipsDiff` | 入场带允许的误差范围。 | `prm.entry.diff` |
| `StopPipsDiff` | 拖尾均线外扩的距离。 | `prm.stop.diff` |
| `StopMaPeriod` | 拖尾均线周期。 | `prm.mastop.period` |
| `StopMaShift` | 拖尾均线回看（移位）根数。 | `prm.mastop.shift` |
| `StopMaMethod` | 均线方法（`MODE_SMA/EMA/SMMA/LWMA`）。 | `prm.mastop.method` |
| `CandleType` | 运行时间框架。 | 图表周期 |

> **点值换算**：若 `Security.PriceStep` 已配置，所有 pip 参数都会自动乘以价格步长；否则按照价格单位直接计算，与 MT4 EA 的回退逻辑一致。

## 使用说明
- 策略基于 `SubscribeCandles`，仅在蜡烛收盘时做出决策；入场带的设计模拟了原 EA 在逐笔行情上的触发条件。
- 拖尾止损通过监控蜡烛高低点来触发市价平仓，等价于 EA 中不停修改的止损单。
- `StopMaShift` 使用先进先出的缓存来取回历史均线值，确保 `0` 表示当前值，正数表示向前回看。
- 每次交易结束后，多空两个状态机会立即复位，防止重复下单，等价于 MT4 中的 `STATUS_TRADE` 重置。

## 快速上手
1. 将策略添加到 StockSharp 环境，并绑定具有正确 `PriceStep` 的交易品种。
2. 通过 `CandleType` 设置时间框架（原 EA 常用于 M5 等分钟级别）。
3. 根据实际报价精度调整各类 pip 距离及拖尾参数。
4. 启动策略，系统会在检测到 Bar A/Bar B 序列时交替做多或做空。

## 与原 EA 的差异
- 本移植版本基于收盘价运行，不再逐笔处理；入场带保持了触发时机的一致性。
- 止损以程序化方式平仓，而非提交/修改 MT4 挂单，符合 StockSharp 常见的实现方式。
- 在 C# 中直接实现了 VMA 指标并保留 `SmoothingPower`，同时移除了 MT4 源码中未使用的 `dK` 参数。
