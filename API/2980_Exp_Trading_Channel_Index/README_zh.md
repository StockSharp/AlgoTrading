
# Exp Trading Channel Index 策略

## 概述
本策略将 MQL5 专家顾问 `Exp_Trading_Channel_Index` 移植到 StockSharp 框架。它跟踪 Trading Channel Index (TCI) 指标，这是一种经过波动性校准的动量指标，会根据数值相对于上下通道的位置将每个柱体着色为五种颜色之一。当历史柱体的颜色发生变化时，策略会在下一根柱体开盘处执行交易，从而完全复刻原始 EA 的行为。

默认使用 H4 蜡烛序列，并且只处理已经收盘的蜡烛。所有交易决策都在确认颜色变化后的下一根蜡烛上做出。

## Trading Channel Index 指标
TCI 由三个步骤组成：

1. **第一阶段平滑**：对选定的价格源应用可配置的移动平均线（SMA、EMA、SMMA、WMA 或 Jurik）得到基准曲线 `XMA`。
2. **波动率估计**：对价格与基准曲线之间的绝对偏差进行平滑。
3. **归一化**：利用系数对偏差进行归一化并再次平滑。结果值与 `HighLevel` 和 `LowLevel` 比较，进而赋予以下颜色编号：
   - `0` – 值高于 `HighLevel`。
   - `1` – 值为正但低于 `HighLevel`。
   - `2` – 值接近零。
   - `3` – 值为负但高于 `LowLevel`。
   - `4` – 值低于 `LowLevel`。

移植版本使用 StockSharp 原生的移动平均指标。对于 Jurik 平滑会使用 `Phase` 参数，其余方法会忽略该参数，与原始脚本保持一致。

## 交易规则
策略检查 `SignalBar` 指定的柱体（默认为上一根已收盘蜡烛）以及它之前的柱体：

- **做多开仓**：两根柱体之前（`SignalBar + 1`）的颜色为 `0`，而上一根柱体的颜色发生变化。如果存在空头仓位则先行平仓，然后按照 `TradeVolume` 开多。
- **做空开仓**：两根柱体之前的颜色为 `4`，而上一根柱体的颜色发生变化。如果存在多头仓位则先平仓，然后开空。
- **平多**：当较早的柱体（两根之前）颜色为 `4` 时执行。
- **平空**：当较早的柱体颜色为 `0` 时执行。

退出逻辑优先于入场逻辑；在开立新仓之前会先关闭相反方向的仓位，这与 `TradeAlgorithms.mqh` 中的辅助函数一致。

## 风险管理
策略按照价格步长设置保护水平：

- `StopLossPoints` 表示入场价与止损价之间的距离，做多时放在下方，做空时放在上方。
- `TakeProfitPoints` 表示入场价与止盈价之间的距离。

在每根收盘蜡烛上检查止损和止盈。如果同时触发，则按照先满足的条件平仓。

## 参数
- **Trade Volume** (`TradeVolume`)：每次开仓的数量。
- **Stop Loss (pts)** (`StopLossPoints`)：以价格步长表示的止损距离。
- **Take Profit (pts)** (`TakeProfitPoints`)：以价格步长表示的止盈距离。
- **Enable Long Entries/Exits** (`BuyPositionOpen`, `BuyPositionClose`)：是否允许多头信号及其平仓。
- **Enable Short Entries/Exits** (`SellPositionOpen`, `SellPositionClose`)：是否允许空头信号及其平仓。
- **Signal Bar** (`SignalBar`)：用于检测颜色变化的历史偏移量。
- **High Level / Low Level** (`HighLevel`, `LowLevel`)：用于着色的阈值。
- **Primary / Secondary Method** (`Method1`, `Method2`)：两阶段平滑使用的移动平均类型。
- **Length #1 / Length #2** (`Length1`, `Length2`)：两阶段平滑的周期长度。
- **Phase #1 / Phase #2** (`Phase1`, `Phase2`)：Jurik 平滑的相位参数（其他方法忽略）。
- **Coefficient** (`Coefficient`)：偏差归一化系数。
- **Applied Price** (`AppliedPrice`)：使用的价格（收盘价、开盘价、最高价、最低价、中值、典型价、加权收盘、简化价、四分位价、趋势跟随价、趋势跟随平均价、Demark 价）。
- **Candle Type** (`CandleType`)：参与计算的蜡烛时间框架。

## 备注
- 根据要求未创建 Python 版本。
- 代码遵循项目的制表符缩进规范，并在关键位置添加了英文注释。
- 自定义的 `TradingChannelIndexValue` 同时提供数值和颜色索引，便于将来扩展可视化。
