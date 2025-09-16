# OsMaMaster 策略

## 概述
OsMaMaster 策略复现了 MetaTrader 4 顾问 **OsMaSter_V0** 的核心行为。策略依靠 MACD 直方图（OsMA）来识别动量反转，只在每根 K 线收盘后做出决策，以符合仓库只处理已完成蜡烛的要求。

## 交易逻辑
- **指标组合**——使用 `MovingAverageConvergenceDivergence` 指标，快速、慢速与信号周期完全对应 MQL 输入（默认 9/26/5）。
- **价格类型**——`AppliedPrice` 参数对应 MetaTrader 的 `PRICE_*` 常量（0=收盘价、1=开盘价、2=最高价、3=最低价、4=中价、5=典型价、6=加权价），所选价格直接送入 MACD。
- **信号识别**——根据 `Shift1`–`Shift4` 所设偏移存储四个 OsMA 值。默认偏移 (0,1,2,3) 用于寻找局部低点或高点：
  - 多头条件：`OsMA[shift4] > OsMA[shift3]`，`OsMA[shift3] < OsMA[shift2]`，`OsMA[shift2] < OsMA[shift1]`；
  - 空头条件：`OsMA[shift4] < OsMA[shift3]`，`OsMA[shift3] > OsMA[shift2]`，`OsMA[shift2] > OsMA[shift1]`。
- **单仓原则**——仅在当前没有持仓时才开新仓，与原始 EA 通过 `ExistPositions` 检查持仓的方式一致。

## 仓位管理
- **止损**——`StopLossPips` 表示入场价到保护止损的点数，设为 `0` 时关闭止损。
- **止盈**——`TakeProfitPips` 与 EA 中的参数完全一致，`0` 表示不使用固定止盈。
- **执行方式**——止损和止盈均按蜡烛的最高价/最低价判断，一旦在蜡烛内部触发，会在收盘时以市价单平仓。
- **状态复位**——平仓后会清空内部的止损/止盈记录，下一笔交易会重新计算并设置。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 计算所用的蜡烛周期。 | 1 小时 |
| `FastEmaPeriod` | MACD 中的快速 EMA 周期。 | 9 |
| `SlowEmaPeriod` | MACD 中的慢速 EMA 周期。 | 26 |
| `SignalPeriod` | MACD 信号 EMA 周期。 | 5 |
| `AppliedPrice` | MetaTrader `PRICE_*` 代码，决定使用哪种价格。 | 0（收盘价） |
| `Shift1` | 第一个 OsMA 偏移，一般代表当前柱。 | 0 |
| `Shift2` | 第二个 OsMA 偏移。 | 1 |
| `Shift3` | 第三个 OsMA 偏移。 | 2 |
| `Shift4` | 第四个 OsMA 偏移。 | 3 |
| `StopLossPips` | 止损点数。 | 50 |
| `TakeProfitPips` | 止盈点数。 | 50 |

## 转换说明
- 策略内部维护一个紧凑的 OsMA 数值缓冲区，避免频繁访问历史数据，符合仓库“不要自建集合”的要求。
- 所有决策都在蜡烛收盘后进行，避免使用未完成的指标数值。
- 止损与止盈通过比较蜡烛极值并使用市价单平仓，复刻 MQL 中在下单时同时设置止损/止盈的效果。
- 默认下单手数设为 **0.01**，与原版顾问的 `Lots` 默认值一致。

## 使用建议
- 可根据标的波动性调整 `CandleType` 和 MACD 周期，快速市场更适合较短的周期。
- 如果希望让盈利尽量奔跑，可将 `TakeProfitPips` 设为 `0`，再配合自定义离场规则。
- 调整 `Shift` 时需保证最大偏移不要过大，因为策略仅维护满足最大偏移所需的 OsMA 值数量。
- 由于平仓依据 K 线数据，采用更短的周期能够降低触发价位与实际平仓之间的延迟。
