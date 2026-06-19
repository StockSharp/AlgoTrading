# Caudate X Period Candle TM Plus 策略

## 概述
该策略复刻 Caudate X Period Candle TM Plus 智能顾问的逻辑。它先对每根蜡烛的开、高、低、收价格进行可配置的平滑处理，再构建平滑 Donchian 区间，并依据蜡烛实体在区间中的位置将已完成蜡烛划分为六种颜色代码。颜色 **0/1**（看涨下影）触发做多，颜色 **5/6**（看跌上影）触发做空，对立的颜色组用于平仓。

## 交易规则
1. 订阅所选时间框架的蜡烛，并按照 `Smoothing Method` 对 OHLC 四个价格序列分别进行平滑。
2. 以 `Donchian Period` 为窗口计算平滑最高价与最低价，再扩展区间以覆盖平滑后的开盘价和收盘价。
3. 根据实体在区间中的相对位置确定颜色：
   * **0/1** – 实体靠近区间上方，形成下影线。
   * **2/4** – 实体位于区间中部。
   * **5/6** – 实体靠近区间下方，形成上影线。
4. 按 `Signal Bar` 指定的偏移读取颜色（默认 `1` 使用上一根完整蜡烛）。
5. 当颜色属于入场组且没有相反方向持仓时开仓。
6. 当颜色属于离场组或持仓时间超过设定上限时平仓。
7. 如设置了止损或止盈距离，策略会通过 `StartProtection` 注册对应的保护指令。

## 参数说明
| 参数 | 说明 |
| --- | --- |
| `Candle Type` | 信号计算所用的时间框架。 |
| `Donchian Period` | 平滑高/低价区间的窗口长度。 |
| `Signal Bar` | 信号回看的条数（0 表示使用当前完成蜡烛）。 |
| `Smoothing Method` | OHLC 平滑方法，可选 SMA、EMA、SMMA、LWMA、Jurik JJMA 近似、Kaufman AMA。 |
| `MA Length` | 平滑滤波器长度。 |
| `MA Phase` | 为 JJMA 兼容保留，当前实现不会使用。 |
| `Enable Long/Short Entries` | 是否允许开多/开空。 |
| `Enable Long/Short Exits` | 是否允许信号触发平多/平空。 |
| `Enable Time Exit` | 是否启用持仓时间上限。 |
| `Time Exit (minutes)` | 强制平仓前允许的持仓分钟数。 |
| `Stop Loss (points)` | 以价格步长为单位的止损距离。 |
| `Take Profit (points)` | 以价格步长为单位的止盈距离。 |

## 注意事项
- `Signal Bar = 1` 对应原始 MQL5 版本的做法，即使用上一根完成的蜡烛给出信号。
- 止损/止盈距离大于零时会调用 `StartProtection`，并基于 `Security.PriceStep` 计算绝对价格偏移。
- `MA Phase` 仅为兼容性保留，StockSharp 自带的均线实现不会使用该值。
- 下单数量通过基类的 `Strategy.Volume` 设置；当满足入场条件时策略会先平掉反向持仓再建立新仓。
