# PresentTrend
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

由收盘价RSI或典型价格/成交量MFI控制的因果ATR线。与两根蜡烛前的线值发生确认交叉时，开仓、完全反转或平仓。

## 线和信号

仅使用已完成的蜡烛，默认五分钟。原生Wilder ATR和所选振荡器使用同一正周期`Length=14`；不注册未选振荡器。两个指标都形成后开始计算：

- 振荡器 ≥50：候选值 `low - Multiplier * ATR`，线为候选值与前一线值的最大值。
- 振荡器 <50：候选值 `high + Multiplier * ATR`，线为候选值与前一线值的最小值。
- 第一个形成的线值就是所选候选值，不假设更早历史。`Multiplier=1`，必须为正。

ATR包含相对前一close的跳空，从增长窗口均值开始，然后Wilder平滑。RSI使用Wilder平滑的close涨跌；close不变时保留前一结果。MFI使用`typical=(high+low+close)/3`乘成交量：typical上涨计正流、下跌计负流、相等均不计。最后`Length`根的MFI为`100 * positiveSum / (positiveSum + negativeSum)`；负流为零时为100。

t时的原始多头交叉要求`line[t-1] <= line[t-3]`且`line[t] > line[t-2]`；空头相应使用≥和<，需四个形成的线值。

多头交叉只有在先前存在原始空头信号，且该空头比先前原始多头更新（或还没有原始多头）时才确认。空头对称。比较之后记录每个原始信号，包括交易被拒绝的信号。没有观测到相反信号时，第一个原始信号不交易；同方向重复原始信号要等相反信号介入后才可再次入场。

## 执行与默认值

双方启用时，从空仓发出`Volume`，或用`Volume + abs(Position)`完全反转相反持仓；同侧信号保持持仓。仅做多时，确认的空头信号完全平多但不开空；仅做空对称。空仓的禁用方向无操作。未完成订单阻止重复，但线和原始信号继续推进。被拒绝信号不排队，无六小时冷却。

没有独立价格止损或止盈。单方向退出是确认的相反信号，不是ATR价格止损订单。

| 参数 | 默认值 |
|---|---:|
| `Length` | 14，正的共享周期 |
| `Multiplier` | 1，正的ATR倍数 |
| `IsUseMfi` | false选择RSI；true选择MFI |
| `IsLongEnabled` / `IsShortEnabled` | true / true，至少启用一方 |
| `CandleType` | 5分钟 |

C#与Python测试在现有BTC/TON历史中独立计算ATR、RSI/MFI、线及原始/确认交叉。检查每个订单和实际持仓的时机、方向、完全反转、单方向平仓；覆盖两种振荡器及间隔小于六小时的信号。不证明盈利。
