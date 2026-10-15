# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

原生Relative Momentum Index配合收盘SMA入场，随后用仅向保护方向收紧的SMA/ATR价格止损。不声称盈利表现。

## 指标和入场

默认使用已完成的五分钟蜡烛。原生RMI：`d[t]=close[t]-close[t-RmiMomentum]`，lag=3。最后`RmiPeriod=21`个动量样本中，U为正d的均值（其余计零），D为负d绝对值的均值（其余计零），`RMI=100*U/(U+D)`。分母为零时无入场值。这是原生滚动均值相对动量计算。

收盘SMA和原生Wilder ATR共享`SuperTrendLength=5`；ATR包含相对前一close的跳空，以增长均值开始，然后Wilder平滑。三个指标形成后，仅从空仓入场：RMI>60且close>SMA做多；RMI<40且close<SMA做空。按策略`Volume`发市价单。持仓期间忽略新的入场信号。

`Direction`是可选的类型化`Sides`过滤器：C#的`null`或Python的`None`表示双向，`Sides.Buy`仅多，`Sides.Sell`仅空。只限制入场，止损始终平掉实际持仓；不使用字符串或数字方向代码。

## 跟踪止损

`width=SuperTrendMultiplier*ATR`，默认倍数4。多头入场的stop=`SMA-width`，后续已完成蜡烛取`max(previousStop,SMA-width)`；空头入场stop=`SMA+width`，随后取`min(previousStop,SMA+width)`。持仓期间从不放松。初始止损来自信号蜡烛的SMA/ATR，不按成交价重算；保留的SuperTrend参数名配置这些以SMA为中心的带。

正bid≤多头stop或正ask≥空头stop时，市价平掉全部实际数量。更新stop后也检查已完成close，当前close到达新stop可立即退出。蜡烛之间的真实报价可触发退出。新stop只用于当前及后续观测价格，不回看该蜡烛此前的high/low。当前RMI为空时，SMA/ATR仍可更新保护。

这是本地价格监控；按报价进行盘中保护需Level1 bid/ask，无报价时仍检查close。不提交交易所原生止损单，成交价可偏离阈值。未完成订单阻止重复，指标和stop继续更新。止损退出的蜡烛不再次入场，稍后的合格蜡烛可重入。没有EMA、六小时冷却、相反信号立即反转或独立止盈。

| 参数 | 默认值 |
|---|---:|
| `RmiPeriod` | 21，正 |
| `RmiMomentum` | 3，正的close lag |
| `SuperTrendLength` | 5，正的共享SMA/ATR周期 |
| `SuperTrendMultiplier` | 4，正的ATR宽度 |
| `Direction` | 未设置，双向 |
| `CandleType` | 5分钟 |

C#和Python在现有BTC/TON历史上独立计算动量和、SMA、Wilder ATR及收紧止损。验证每个入场、可执行价格退出和实际持仓、两方向及过滤、持仓中忽略相反入场、紧止损的蜡烛间触发和小于六小时的入场间隔。
