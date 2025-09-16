# Rabbit M3

## 概述
Rabbit M3 是 MetaTrader 4 智能交易系统 `RabbitM3`（又名“Petes Party Trick”）的移植版本。策略通过一对一小时指数移动平均线判断市场处于只做多还是只做空的状态；当快线在慢线之上时只寻找多头信号，在其之下时只允许空头信号。入场需要 Williams %R 动能穿越配合 CCI 水平过滤，而一个超长的唐奇安通道用于侦测价格是否突破原有趋势。策略还保留了原程序在大额盈利后提升下次手数的规则。

## 策略逻辑
### 趋势状态过滤
* 当快 EMA 收盘价低于慢 EMA 时，立即平掉现有多单，并且之后只会寻找空头机会。
* 当快 EMA 收盘价高于慢 EMA 时，立即平掉现有空单，并且之后只会寻找多头机会。
* 若两条 EMA 相等，则保持上一根柱子的状态，与原始 EA 仅在严格大小关系变化时才切换模式的做法一致。

### 入场规则
* **做空**
  * 当前状态必须为只做空（快 EMA < 慢 EMA）。
  * Williams %R（周期 = `WilliamsPeriod`）最新一根K线向下穿越 `WilliamsSellLevel`，且前一根数值仍小于0。
  * CCI（周期 = `CciPeriod`）必须大于等于 `CciSellLevel`。
  * 当前净仓位必须为空；策略最多只持有 `MaxOpenPositions` 笔仓位，默认以 `EntryVolume` 手数市价开仓。
* **做多**
  * 当前状态必须为只做多（快 EMA > 慢 EMA）。
  * Williams %R 向上穿越 `WilliamsBuyLevel`，且前一根数值仍小于0。
  * CCI 必须小于等于 `CciBuyLevel`。
  * 入场前净仓位必须为空。

### 出场规则
* **固定止盈止损** – `StopLossPips` 与 `TakeProfitPips` 会根据标的的价格最小变动单位转换成价格距离，设置为 `0` 即代表禁用该保护。
* **唐奇安突破** – 若收盘价高于上一根唐奇安上轨（周期 = `DonchianLength`），则立即平掉空单；收盘价低于上一根下轨时立即平掉多单。使用上一根轨道值以复现 EA 中 `shift=1` 的调用方式。
* **趋势反转** – 每当 EMA 关系翻转，策略会先平掉反向仓位，再允许按照新的方向寻找信号。

### 资金管理
* 初始每次开仓数量为 `EntryVolume`。
* 当平仓实现盈利超过 `BigWinThreshold` 且当前没有持仓时，下一次下单量会增加 `VolumeIncrement`，同时阈值翻倍（4 → 8 → 16 ……）。如果任一参数设为 `0` 则关闭此手数递增机制。

## 参数
* **Fast EMA Period** – 快速趋势过滤器周期（默认 33）。
* **Slow EMA Period** – 慢速趋势过滤器周期（默认 70）。
* **Williams %R Period** – Williams %R 动能指标周期（默认 62）。
* **Williams Sell Level** – 触发空头信号的向下穿越水平（默认 −20）。
* **Williams Buy Level** – 触发多头信号的向上穿越水平（默认 −80）。
* **CCI Period** – 商品通道指数周期（默认 26）。
* **CCI Sell Level** – 允许做空的最低 CCI 值（默认 101）。
* **CCI Buy Level** – 允许做多的最高 CCI 值（默认 99）。
* **Donchian Length** – 唐奇安通道取值的历史长度（默认 410）。
* **Max Open Positions** – 同时持仓的最大数量，原策略为 1（默认 1）。
* **Take Profit (pips)** – 以点数表示的止盈距离（默认 360）。
* **Stop Loss (pips)** – 以点数表示的止损距离（默认 20）。
* **Entry Volume** – 初始下单手数（默认 0.01）。
* **Big Win Threshold** – 激活加仓机制所需的单次实现盈利（默认 4.0）。
* **Volume Increment** – 达到阈值后增加的手数（默认 0.01）。
* **Candle Type** – 指标使用的K线周期（默认 1 小时）。

## 补充说明
* 点值换算依赖于品种的 `PriceStep`，若未提供则退化为 1 个价格单位。
* 唐奇安通道故意滞后一根K线，以保持与原始 `iHighest`/`iLowest` 的偏移一致。
* 手数递增逻辑只在仓位清零、且产生实现盈亏时评估，避免浮动盈利带来误判。
* 原 EA 中用于显示信息的图形对象未移植，在 StockSharp 中可以通过图表和日志查看状态。
* 本目录仅提供 C# 版本，没有 Python 实现。
