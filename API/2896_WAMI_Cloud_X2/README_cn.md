# WAMI Cloud X2
[English](README.md) | [Русский](README_ru.md)

该策略完整复刻 MetaTrader 智能交易程序 “Exp_WAMI_Cloud_X2” 的双周期逻辑。高周期上使用 Warren Momentum Indicator (WAMI) 判断趋势方向，低周期上再次计算 WAMI 用于择时。主线与信号线在两个周期上都会互相比较，从而完全对应原始 MQL 程序的行为。

## 策略思想

- **WAMI 结构**：WAMI 基于收盘价的一阶差分，依次通过三层可选类型的移动平均（SMA、EMA、SMMA、LWMA）进行平滑，再用第四个移动平均生成信号线。策略内置的自定义指标按相同顺序计算，并在一次返回值中同时提供主线和信号线。
- **趋势过滤（高周期）**：默认使用 6 小时 K 线。若主线高于信号线，则趋势判定为多头；低于则判定为空头；在两线相等或指标尚未形成时保持中性。
- **信号引擎（低周期）**：默认使用 30 分钟 K 线寻找交易机会。每根完成的 K 线都会将最新的 WAMI 值压入缓存，然后按照 `SignalBar` 参数指定的已收盘柱进行交叉检测，即比较最近柱（`SignalBar`）与更早一柱（`SignalBar + 1`）。

## 交易规则

1. **离场条件**
   - 当低周期 WAMI 持续表现为空头 (`previous.Main < previous.Signal`) 且 `CloseLongOnSignal` 开启时，平掉所有多头仓位。
   - 当低周期 WAMI 持续表现为多头 (`previous.Main > previous.Signal`) 且 `CloseShortOnSignal` 开启时，平掉所有空头仓位。
   - 一旦高周期趋势翻转，`CloseLongOnTrendFlip` 或 `CloseShortOnTrendFlip` 会强制关闭对应方向的仓位。
2. **入场条件**
   - 在高周期为空头的前提下，只要低周期主线从下向上穿越信号线 (`current.Main >= current.Signal` 且 `previous.Main < previous.Signal`)，并且 `EnableSellEntries` 为真，就开空。这与原始 EA 在下跌趋势中捕捉第一次向上穿越信号线的做法一致。
   - 多头信号与其相反：高周期为多头，同时低周期主线从上向下穿越信号线 (`current.Main <= current.Signal` 且 `previous.Main > previous.Signal`)，并且 `EnableBuyEntries` 为真时开多。
   - 如果已经持有反向仓位，策略会发送一次性市价单，先平掉现有头寸再按 `TradeVolume` 开新仓，从而实现快速反手。

## 参数说明

- **趋势 WAMI**：`TrendPeriod1/2/3`、`TrendMethod1/2/3`、`TrendSignalPeriod`、`TrendSignalMethod`、`TrendCandleType`。
- **信号 WAMI**：`SignalPeriod1/2/3`、`SignalMethod1/2/3`、`SignalSignalPeriod`、`SignalSignalMethod`、`SignalCandleType`。
- **控制开关**：`SignalBar`、`EnableBuyEntries`、`EnableSellEntries`、`CloseLongOnTrendFlip`、`CloseShortOnTrendFlip`、`CloseLongOnSignal`、`CloseShortOnSignal`。
- **下单数量**：`TradeVolume` 控制每次新开仓的手数，若需要反手，会在平仓数量基础上额外加上该值。

所有输入都通过 `StrategyParam<T>` 创建，可在 StockSharp 前端直接修改或用于参数优化，完全对应原始 EA 的设置方式。

## 默认设置

- **趋势周期**：6 小时。
- **信号周期**：30 分钟。
- **移动平均类型**：全部为简单移动平均 (SMA)。
- **移动平均长度**：三段平滑分别为 4 / 13 / 13，信号线长度为 4（两个周期均相同）。
- **SignalBar**：1（使用最近一根已收盘 K 线）。
- **TradeVolume**：1 手。
- **所有权限开关**：默认开启。

## 其他说明

- 策略不会自动下达止损或止盈单，需要时请结合独立的风险管理模块。
- 图表会绘制信号周期的 K 线、两条 WAMI 曲线以及交易记录；趋势周期显示在单独的图表区域，方便人工核对。
- 实现完全采用蜡烛订阅和 `BindEx` 回调，没有直接调用指标的 `GetValue` 等低级接口，满足项目的高层 API 要求。
