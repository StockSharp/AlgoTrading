# Exp Skyscraper Fix Duplex 策略

## 概述
Exp Skyscraper Fix Duplex 是 MQL5 专家顾问 *Exp_Skyscraper_Fix_Duplex* 的移植版本。策略分别为多头与空头侧构建 Skyscraper Fix 渠道，每一侧都可以使用不同的时间框架、ATR 窗口与灵敏度，从而在 StockSharp 内部以同一交易框架应对不同的市场状态。

## 指标逻辑
自定义 **Skyscraper Fix** 指标完整复刻了原脚本：

- 对每根收盘的 K 线计算周期固定为 15 的 ATR。
- 在可配置的 `Length` 窗口内求 ATR 的最高值和最低值，用来确定自适应的价格步长。
- 根据 `Mode` 选择使用 High/Low 还是 Close 来向外投射上下轨，距离为两个步长。
- 当收盘价突破上轨或下轨时，内部趋势翻转，同时锁定相反一侧的轨道，使其不再逆势移动。
- 另一侧轨道被触及时产生离散的买入或卖出触发信号，对应于 MQL 指标中的箭头缓冲区。

指标输出上轨、下轨、买入触发、卖出触发以及可选的中线，方便绘图或调试。

## 交易规则
多头与空头在各自订阅的完成 K 线上独立评估：

- **多头开仓** – 当多头指标给出新的买入触发时执行。若存在空头仓位会先行平仓，然后按照设定的交易量以市价买入。
- **多头平仓** – 当多头指标给出下轨值时，现有多头仓位将以市价卖出。
- **空头开仓** – 当空头指标给出新的卖出触发时执行。若存在多头仓位会先行平仓，然后以市价卖出建立空头。
- **空头平仓** – 当空头指标给出上轨值时，现有空头仓位将以市价买入平仓。

`SignalBar` 参数可以让信号延迟若干根已收 K 线处理，`1` 与原始 MQL 默认行为一致，`0` 则直接基于最新收盘线行动。

## 参数
- `TradeVolume` – 市价下单的交易量。
- `EnableLongEntries` / `EnableLongExits` – 是否允许多头开仓 / 平仓。
- `LongCandleType` – 多头指标使用的 K 线类型。
- `LongLength`、`LongKv`、`LongPercentage`、`LongMode`、`LongSignalBar` – 多头侧的 Skyscraper Fix 设置。
- `EnableShortEntries` / `EnableShortExits` – 是否允许空头开仓 / 平仓。
- `ShortCandleType` – 空头指标使用的 K 线类型。
- `ShortLength`、`ShortKv`、`ShortPercentage`、`ShortMode`、`ShortSignalBar` – 空头侧的 Skyscraper Fix 设置。

## 使用说明
- `TradeVolume` 会同步到策略的 `Volume` 属性，因此 `BuyMarket()` / `SellMarket()` 会自动采用该数量下单。
- 指标会读取标的的 `PriceStep`。如果步长为零，指标会等待直到获得有效的价格步长再输出信号。
- 启动时会调用 `StartProtection()`，确保在第一笔交易之前激活内置保护机制。
- 按照任务要求未提供 Python 版本，因此没有 `PY` 目录。
