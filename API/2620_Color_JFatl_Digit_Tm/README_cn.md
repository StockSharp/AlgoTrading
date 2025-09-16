# Color JFATL Digit TM 策略

## 概览
**Color JFATL Digit TM 策略** 是对原始 MetaTrader 5 专家的移植版本。策略通过对 FATL（Fast Adaptive Trend Line）进行 Jurik 平滑处理，并根据曲线斜率的“颜色”变化来决定交易方向，同时支持自定义交易时段、止损和止盈。每根已完成的 K 线都会被标记为：上涨（颜色 = 2）、下跌（颜色 = 0）或中性（颜色 = 1）。颜色的转换会触发建仓、平仓与持仓管理。

## 核心逻辑
1. **自定义指标复现**
   - 依据原始指标的 39 个权重，对选定的价格类型进行卷积，得到 FATL 值。
   - 使用 StockSharp 的 `JurikMovingAverage` 进行平滑；若运行库公开 `Phase` 属性，则通过反射配置它，以贴近 MT5 的参数行为。
   - 将平滑后的值按 `Security.PriceStep × 10^DigitRounding` 进行量化，复现 MQL5 中的 `Digit` 输入。
   - 当前值与上一值的差异决定颜色：上升为 2、下降为 0、无变化则继承上一颜色（默认为 1）。

2. **信号判定**
   - 颜色值存储在循环缓冲区中，`SignalBar` 参数决定忽略多少根已完成的 K 线（默认 1，即上一根收盘线）。
   - **做多开仓**：前一颜色为 2，而最近颜色 < 2。
   - **做空开仓**：前一颜色为 0，而最近颜色 > 0。
   - **多头平仓**：当前一颜色变为 0 时触发。
   - **空头平仓**：当前一颜色变为 2 时触发。
   - 若当前已有仓位，则跳过开仓信号，维持与 MT5 原策略相同的单仓位模式。

3. **时段控制与风控**
   - `EnableTimeFilter` 复制了 MT5 的时段逻辑，包含跨日情形（开始时段大于结束时段）。
   - 若当前时间不在允许的交易窗口内，策略会立即平掉所有仓位，与原专家一致。
   - 止损与止盈以“点”为单位输入，通过价格步长换算为价格后传给 `StartProtection`。

## 参数说明
- `OrderVolume`：每次下单的数量。
- `EnableTimeFilter`、`StartHour`、`StartMinute`、`EndHour`、`EndMinute`：交易时段设置。
- `StopLossPoints`、`TakeProfitPoints`：止损与止盈距离（点），设为 0 表示禁用。
- `BuyOpenEnabled`、`SellOpenEnabled`、`BuyCloseEnabled`、`SellCloseEnabled`：分别控制多/空的开仓与平仓信号是否生效。
- `SignalCandleType`：用于计算指标与信号的 K 线周期（默认 4 小时）。
- `JmaLength`、`JmaPhase`：Jurik 平滑参数（若底层实现不支持 `Phase` 则自动忽略）。
- `AppliedPriceMode`：与 MT5 指标一致的价格枚举（收盘价、开盘价、中值、趋势跟随价、Demark 价等）。
- `DigitRounding`：指标值量化时的倍数，等同于 MQL 指标的 `Digit` 输入。
- `SignalBar`：信号计算时回溯的已完成 K 线数量（默认 1）。

## 注意事项
- 策略使用 `SubscribeCandles` 与高层次下单接口（`BuyMarket`、`SellMarket`），符合转换指南的要求。
- Jurik 平滑的相位通过反射赋值；若运行环境不提供该属性，则采用默认行为。
- 若 `Security.PriceStep` 不可用，指标值不会进行量化。
- 根据需求未提供 Python 版本。

## 使用方法
1. 连接到能够提供 `SignalCandleType` 周期行情的数据源，并将策略附加到目标标的。
2. 配置适合的价格类型、Jurik 参数、交易时段及风控参数。
3. 启动策略，策略会在单一仓位框架下，根据上述颜色转换逻辑执行下单和平仓，并应用止损/止盈保护。
