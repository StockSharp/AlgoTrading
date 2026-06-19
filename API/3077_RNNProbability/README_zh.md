# RNN Probability 策略

## 概述
RNN Probability 策略移植自 MetaTrader 专家顾问 *RNN (barabashkakvn's edition)*。原始算法会采集三个间隔等于 RSI 周期的 RSI 数值，并将其送入一个手工构建的概率网络，模拟递归神经网络的判定逻辑。StockSharp 版本通过高级别的蜡烛订阅复现这一流程，并自动将 MetaTrader 中的手数、点值以及止损/止盈距离转换成 StockSharp 的概念。

当最新完成的蜡烛生成 RSI 值后，策略会回溯一倍和两倍 RSI 周期的历史 RSI 数据。三个归一化的数值与八个权重（`Weight0` … `Weight7`）组合，得出市场下行的概率。该概率被线性映射到 `[-1; 1]` 区间，符号决定做多还是做空。策略始终只持有一个方向的净头寸，与原始 EA 保持一致。

## 交易逻辑
1. 订阅所选蜡烛类型，并使用 `AppliedPrice`（默认取开盘价）作为输入手动推进 `RelativeStrengthIndex` 指标。
2. 将完成的 RSI 数值保存在一个滚动缓冲区，确保可以访问一倍和两倍周期之前的数据。
3. 将三个 RSI 数值归一化到 `[0; 1]` 范围后计算概率网络：
   - 当当前 RSI 处于区间下半部分（低于 50）时，使用 `Weight0`、`Weight1`、`Weight2`、`Weight3` 组合。
   - 当当前 RSI 位于区间上半部分时，使用 `Weight4`、`Weight5`、`Weight6`、`Weight7` 组合。
4. 将得到的概率转换为 `-1` 到 `+1` 之间的信号。
5. 若当前无持仓且信号为负，则买入 `TradeVolume` 手；若信号为零或正，则卖出同样的手数。
6. 可以选择按点数同时设置止损和止盈。策略会根据 `PriceStep` 自动换算成绝对价差，并包含 MetaTrader 在 3/5 位报价上使用的额外乘数。
7. 每次决策都会写入日志，记录 RSI 输入、概率及最终信号，方便复查。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 小时时间框架 | 用于生成信号和指标数据的主蜡烛序列。 |
| `TradeVolume` | `decimal` | `1` | 每次下单的手数。 |
| `RsiPeriod` | `int` | `9` | RSI 指标周期，同时决定历史采样间隔。 |
| `AppliedPrice` | `AppliedPriceType` | `Open` | 送入 RSI 的价格类型（开盘、收盘、最高、最低、中值、典型价、加权价等）。 |
| `StopLossTakeProfitPips` | `decimal` | `100` | 止损和止盈的点数距离，填 0 可禁用保护单。 |
| `Weight0` … `Weight7` | `decimal` | `6, 96, 90, 35, 64, 83, 66, 50` | 概率网络的八个权重，取值范围 0~100。 |

## 与原始 MetaTrader 专家的差异
- 移除了邮件通知功能；StockSharp 的日志足以提供同样的反馈。
- 持仓量固定为 `TradeVolume`，未实现部分平仓或逐步加仓，与原始代码保持一致。
- 指标数据通过高级蜡烛订阅提供，无需手动调用 `CopyBuffer` 或操作指针。
- 点值换算直接使用品种的 `PriceStep`，并在 3/5 位报价上自动乘以 10，而不是写死点差。

## 使用建议
- 启动前请确认 `TradeVolume` 与品种的最小手数步长一致；构造函数同时将该值写入 `Strategy.Volume`。
- 在优化过程中调节八个权重，可以让概率网络适应不同市场环境。
- 在点差较大的品种或计划手动离场时，可减小 `StopLossTakeProfitPips` 或设为 0。
- 将策略添加到图表，可同时查看蜡烛、RSI 以及成交，便于验证神经网络输出。

## 指标
- 一个根据所选价格计算的 `RelativeStrengthIndex`。
