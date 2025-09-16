# Blau SM Stochastic 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
该策略是将 MetaTrader 5 专家 `Exp_BlauSMStochastic` 迁移到 C# 的版本。核心使用 Blau SM Stochastic 振荡指标，通过测量价格相对于最近区间的位置并进行多次平滑来生成信号。策略仅在完全收盘的K线（默认 4 小时时间框架）上运作，可同时进行多头和空头交易。

## 指标逻辑
1. 在最近 `LookbackLength` 根K线中计算最高价和最低价。
2. 构建去趋势序列：`sm = price - (HH + LL) / 2`，其中 `price` 为所选价格类型。
3. 使用 `SmoothMethod` 指定的移动平均（SMA、EMA、SMMA、LWMA）依次对 `sm` 进行三次平滑，周期分别为 `FirstSmoothingLength`、`SecondSmoothingLength` 和 `ThirdSmoothingLength`。
4. 将半区间 `(HH - LL) / 2` 通过相同的三次平滑序列以归一化波动率。
5. 主振荡线定义为 `100 * smoothed(sm) / smoothed(range)`。
6. 使用 `SignalLength` 对主线再次平滑得到信号线。

参数 `Phase` 仅用于与原始 MQL 版本保持一致，在当前简化的平滑实现中未参与计算。

## 交易模式
- **Breakdown**：监控主线穿越零值。当主线从正区间进入非正区间时开多并平空；从负区间进入非负区间时开空并平多。
- **Twist**：监控动量拐点。主线在下行后出现上拐（局部低点）时开多并平空；主线在上行后出现下拐（局部高点）时开空并平多。
- **CloudTwist**：观察主线与信号线的交叉。主线自上而下穿越信号线时开多并平空；自下而上穿越时开空并平多。

`EnableLongEntry`、`EnableShortEntry`、`EnableLongExit`、`EnableShortExit` 四个开关可分别控制开仓或平仓动作，而不会影响指标计算。

## 风险管理
`TakeProfitPoints` 与 `StopLossPoints` 会根据标的的价格步长转换为绝对价格距离，并通过 `StartProtection` 启动保护模块。设为 0 即关闭对应的止盈或止损。

## 参数说明
- `CandleType` *(DataType，默认 4 小时)*：用于订阅和计算的K线类型。
- `Mode` *(BlauSmStochasticMode，默认 Twist)*：信号生成模式（Breakdown、Twist、CloudTwist）。
- `SignalBar` *(int，默认 1)*：在评估信号时向后取值的K线数量，对应原始参数 `SignalBar`。
- `LookbackLength` *(int，默认 5)*：计算最高价、最低价所用的回溯长度。
- `FirstSmoothingLength` *(int，默认 20)*：第一阶段平滑周期。
- `SecondSmoothingLength` *(int，默认 5)*：第二阶段平滑周期。
- `ThirdSmoothingLength` *(int，默认 3)*：第三阶段平滑周期。
- `SignalLength` *(int，默认 3)*：信号线的平滑周期。
- `SmoothMethod` *(BlauSmSmoothMethod，默认 EMA)*：所有平滑阶段使用的均线类型（SMA、EMA、SMMA、LWMA）。
- `PriceType` *(BlauSmAppliedPrice，默认 Close)*：振荡器使用的价格类型（收盘、开盘、最高、最低、中位、典型、加权、简单、四分位、TrendFollow 两种及 Demark）。
- `EnableLongEntry` *(bool，默认 true)*：允许开多。
- `EnableShortEntry` *(bool，默认 true)*：允许开空。
- `EnableLongExit` *(bool，默认 true)*：允许平多。
- `EnableShortExit` *(bool，默认 true)*：允许平空。
- `TakeProfitPoints` *(int，默认 2000)*：以点数表示的固定止盈距离。
- `StopLossPoints` *(int，默认 1000)*：以点数表示的固定止损距离。

## 说明
- 当前平滑引擎仅支持经典均线（SMA、EMA、SMMA、LWMA），MQL 库中的 JMA、JurX 等特殊算法在 StockSharp 中不可用。
- `Phase` 参数仅为兼容保留，调节不会影响结果。
- 策略可用于任意 StockSharp 支持的交易品种，建议根据标的波动特性调整时间框架、平滑周期以及止盈止损参数。
