# BandOsMaCustom 策略

## 概述

该策略移植自 MetaTrader 5 专家顾问
`MQL/45596/mql5/Experts/MQL5Book/p7/BandOsMACustom.mq5`。原始 EA 将 MACD
柱状图（OsMA）与其上的布林带以及一条额外的移动平均线结合起来，但所有
计算都针对柱状图数值而不是价格。当 OsMA 跌破下轨时开多仓，触及上轨时开
空仓；随后柱状图与移动平均线的交叉会触发平仓。同时 EA 使用固定止损与
“止损/50” 的追踪步长来保护仓位。

StockSharp 版本保持了相同的逻辑，并使用高级 API 使代码在框架内更易读、更
容易调试。

## 转换要点

* 使用 `MovingAverageConvergenceDivergenceHistogram` 重现 MetaTrader 的 `iOsMA`
  输出，输入价格由 `AppliedPrice` 参数决定，对应 `PRICE_*` 取值。
* 布林带和退出移动平均线对 OsMA 输出进行处理。为了模拟 MQL 中的
  `BandsShift` 与 `MaShift`，策略维护了紧凑的历史缓冲区，而无需通过索引访问
  指标值。
* 买入/卖出信号与原始 EA 完全一致：跌破下轨生成多头信号，突破上轨生成空头
  信号，OsMA 穿越退出均线则清除信号，从而允许平仓。
* 使用 `StartProtection` 设置止损和追踪止损，追踪步长保持为 `StopLossPoints / 50`
  个价格步长，与 MQL 的 `TrailingStop` 类一致。

## 指标

| 指标 | 作用 |
| --- | --- |
| `MovingAverageConvergenceDivergenceHistogram` | 复现 MetaTrader 的 `iOsMA` 柱状图。 |
| `BollingerBands` | 在柱状图上构建布林带上下轨。 |
| 可选移动平均 (SMA/EMA/SMMA/LWMA) | 当柱状图穿越时触发退出。 |

## 参数

| 名称 | 默认值 | 描述 |
| --- | --- | --- |
| `CandleType` | 1 小时时间框架 | 所有指标的基础周期。 |
| `FastOsmaPeriod` | 12 | OsMA 快速 EMA 周期。 |
| `SlowOsmaPeriod` | 26 | OsMA 慢速 EMA 周期。 |
| `SignalPeriod` | 9 | OsMA 信号 SMA 周期。 |
| `AppliedPrice` | Typical | 输入 OsMA 的价格类型（对应 MetaTrader `PRICE_*`）。 |
| `BandsPeriod` | 26 | 计算布林带的周期。 |
| `BandsShift` | 0 | 布林带向右平移的柱数。 |
| `BandsDeviation` | 2.0 | 布林带标准差倍数。 |
| `MaPeriod` | 10 | 用于退出的移动平均周期。 |
| `MaShift` | 0 | 退出均线的平移柱数。 |
| `MaMethod` | Simple | 移动平均类型（SMA/EMA/SMMA/LWMA）。 |
| `StopLossPoints` | 1000 | 以价格步长表示的止损距离。 |
| `OrderVolume` | 0.01 | 交易手数，对应 MetaTrader 中的 “Lots”。 |

## 交易规则

1. 订阅所选周期的蜡烛数据，将选定的价格类型送入 OsMA 指标。
2. 将每次计算出的 OsMA 值传递给布林带和退出移动平均线。
3. 使用带有平移的缓存判断信号：
   * OsMA 从上方跌破下轨 → 产生多头信号。
   * OsMA 从下方突破上轨 → 产生空头信号。
   * OsMA 穿越退出均线 → 清除当前信号，从而允许平仓。
4. 仓位管理：
   * 当多头信号消失时平掉多头；当空头信号消失时平掉空头。
   * 无持仓且存在多头信号时买入，无持仓且存在空头信号时卖出。
5. 通过 `StartProtection` 设置止损，并按照 `StopLossPoints / 50` 的追踪步长启动
   追踪止损。

## 备注

* 源码中的注释全部为英文，以满足仓库规定。
* 历史缓冲区避免了直接索引指标值，同时完整支持 MetaTrader 的 `BandsShift`
  与 `MaShift` 设置。
* 策略完全基于 StockSharp 高级 API：`SubscribeCandles` 驱动指标更新，
  `BuyMarket`/`SellMarket` 直接对应原始 EA 的市价单逻辑。
