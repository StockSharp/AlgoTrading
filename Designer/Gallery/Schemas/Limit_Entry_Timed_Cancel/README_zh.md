# 带定时撤单的MFI限价入场
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该仅做多图展示一张挂单的完整生命周期。MFI(14)离开超卖区后在收盘价下方登记买单，N values计数五根已完成K线，Order cancellation撤销未成交订单，成交则通过旧版Trades for order进入1%/1%持仓保护。

![schema](schema.svg)

## 策略概览

- MFI向上穿越20，模拟源代码记住曾进入超卖区的状态。
- Position == 0时，在Close × (1 − 0.5/100)登记一单位买入限价。
- 单生命周期标志启动五根K线计时，并在结束前拒绝新订单，避免旧超时撤销新订单。
- 成交时Trades for order把本订单成交传给保护；未成交时计时器撤销准确的登记订单。

## 入场与出场规则

- **做多入场**: MFI从20下方上穿且仓位为空时，在已完成收盘价下方0.5%放置买入限价。
- **做空入场**: 没有做空入场，与C#一致。
- **离场**: 成交后在+1%或−1%退出；未成交订单在五根已完成K线后撤销，信号K线按源代码立即增加年龄的方式计为第一根。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | MFI、定价和撤单计数使用的已完成K线周期。 |
| MFI Period | 14 | MoneyFlowIndex使用的K线数。 |
| MFI Oversold Level | 20 | 向上穿越后启动做多的MFI水平。 |
| Replay Entry Offset, % | 0.5 | 图中低于close的距离；C#默认0.1%，0.5%用于回放展示撤单。 |
| Order Volume | 1 | 唯一挂单买入的数量。 |
| Cancel After Candles | 5 | 从登记到尝试撤单所计的已完成K线数。 |
| Take Profit, % | 1 | 持仓保护相对成交价的盈利百分比。 |
| Stop Loss, % | 1 | 持仓保护相对成交价的亏损百分比。 |

## 图表详情

- C#默认偏移0.1%。蜡烛撮合在价格位于Low..High时成交，五分钟BTCUSDT上几乎立即成交；本图用0.5%让定时撤单可见，并在此说明源值。
- Trades for order已弃用，Designer建议使用Order registering的Trades输出。本画廊仅在此展示一次，不应复制到新图。
- 生命周期标志由计时器而非成交重置，因此仍运行的旧计时器不可能通过Order插口撤销替代的新订单。
- 源代码有20根K线信号冷却；紧凑图省略独立冷却，但五根K线生命周期锁仍防止挂单重叠。
- 尽管源目录名称含有Averaging，可执行C#并无加仓订单。本图同样只有一个入场且按评审要求没有Chart panel。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
