# Exp BlauCMI

## 概述
该策略在 StockSharp 的高级 API 上复刻 MetaTrader 5 智能交易系统 **Exp_BlauCMI**。它计算 Blau Candle Momentum Index（CMI），这是一个三重平滑的动量比率，用于捕捉动量拐点。系统在蜡烛线收盘后做出决策：当指标在下行后转向上行时开多，当指标在上行后转向下行时开空，从而完全对应原始 EA 的行为。

## 指标逻辑
1. 通过 `Momentum Price` 与 `Reference Price` 选择两个价格序列。原始动量等于当前领先价格与滞后价格（由 `Momentum Depth` 决定的延迟）之差。
2. 动量及其绝对值依次经过三次平滑（`First/Second/Third Smoothing`），每个阶段使用相同的移动平均法：简单、指数、平滑（RMA）或线性加权。
3. Blau CMI 的公式为 `100 * smoothedMomentum / smoothedAbsMomentum`。当第三次平滑累积足够的历史数据后开始生成信号。
4. `Signal Shift` 指定在检测拐点时向后查看的已收盘蜡烛数量（默认 1，对应原始 EA 使用上一根已收盘的蜡烛）。

## 交易规则
- **做多入场**：`Allow Long Entry` 为 true 且满足 `Value[Signal Shift - 1] < Value[Signal Shift - 2]`、`Value[Signal Shift] > Value[Signal Shift - 1]` 时触发，表示 CMI 由下转上。如存在空头仓位且允许 `Allow Short Exit`，先行平仓。
- **做空入场**：`Allow Short Entry` 为 true 且满足 `Value[Signal Shift - 1] > Value[Signal Shift - 2]`、`Value[Signal Shift] < Value[Signal Shift - 1]` 时触发，表示 CMI 由上转下。如存在多头仓位且允许 `Allow Long Exit`，先行平仓。
- **多头离场**：在持多情况下出现做空信号且 `Allow Long Exit` 为 true 时平多。
- **空头离场**：在持空情况下出现做多信号且 `Allow Short Exit` 为 true 时平空。
- 所有交易均使用市场单，并以 `Order Volume` 指定的数量下单。`StartProtection` 自动为仓位附加止损和止盈，在仓位关闭前一直有效。

## 参数
- `Candle Type` – 用于计算和决策的蜡烛类型（时间框架等），默认 4 小时。
- `Smoothing Method` – 三个平滑阶段共用的平均算法（Simple、Exponential、Smoothed、Linear Weighted）。
- `Momentum Depth` – 计算原始动量时前后价格之间的间隔。
- `First/Second/Third Smoothing` – 三个平滑阶段的长度，同时应用于动量及其绝对值。
- `Signal Shift` – 检测拐点时回看的已收盘蜡烛数量（最小值 1）。
- `Momentum Price` – 动量领先腿使用的价格类型。
- `Reference Price` – 动量滞后腿使用的价格类型。
- `Allow Long Entry`、`Allow Short Entry` – 是否允许开多或开空。
- `Allow Long Exit`、`Allow Short Exit` – 是否允许在相反信号出现时平掉对应方向的仓位。
- `Stop-Loss Points`、`Take-Profit Points` – 以价格最小变动单位 (`Security.PriceStep`) 为度量的止损/止盈距离，设为 0 则关闭该保护。
- `Order Volume` – 市场单的下单数量，并同步赋值给策略的 `Volume` 属性。

## 补充说明
- 支持的平滑方法对应 StockSharp 中的 SMA、EMA、Smoothed MA（RMA）和 WMA 指标。
- Demark 价格常量完全遵循 MT5 版本：先对最高、最低与收盘进行加权平均，再计算与高低点的距离。
- 策略仅在蜡烛收盘后运行一次，因此触发频率与原 EA 使用 `IsNewBar` 的机制一致。
- `Stop-Loss Points` 与 `Take-Profit Points` 被解释为价格步长的倍数，保持与原始 MQL5 参数的“点数”设定兼容。
