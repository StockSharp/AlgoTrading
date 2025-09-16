# Ultra Absolutely No Lag LWMA 策略

## 概览

**Ultra Absolutely No Lag LWMA 策略** 使用 StockSharp 高级 API 重现同名 MetaTrader 专家的信号。指标链对价格进行双重加权移动平均平滑，然后通过一组长度逐步增加的平滑器来统计向上与向下的阶段数量。最后的统计值再次平滑并转换成颜色状态，用于驱动交易逻辑。策略可在进场后自动下达止损和止盈保护单。

## 指标流程

1. **双 LWMA 过滤**：默认对收盘价执行两次加权移动平均，过滤短期噪声。
2. **阶梯式平滑器**：将过滤后的数据输入到可配置数量的平滑器，每个阶段的周期在 `StartLength` 基础上增加固定步长，默认使用 Jurik 平滑。
3. **多头/空头计数**：比较每个阶段当前值与上一个值，向上则计入多头，向下则计入空头。
4. **最终平滑**：对多头和空头计数再次平滑，得到最终的指标状态。

策略复刻原始指标的颜色逻辑：强势多头输出代码 7–8，普通多头输出 5–6，强势空头输出 1–2，普通空头输出 3–4，0 表示尚未形成的状态。

## 交易规则

* 当较旧的柱子产生多头代码（`> 4`），而最近柱子切换到空头代码（`< 5` 且不为 0）时，策略先平掉空头头寸，并可开立新的多头。
* 当较旧的柱子产生空头代码（`< 5` 且不为 0），而最近柱子切换到多头代码（`> 4`）时，策略先平掉多头头寸，并可开立新的空头。
* 如果设置了 `StopLossOffset` 或 `TakeProfitOffset`，每次进场都会注册对应的保护单。

策略始终使用指示周期内最近的两个已完成 K 线，与原始专家在收盘价触发信号的逻辑保持一致。

## 参数

| 参数 | 说明 |
| ---- | ---- |
| `CandleType` | 指标计算所使用的K线类型/周期。|
| `BaseLength` | 双重 LWMA 过滤的周期。|
| `AppliedPriceMode` | 输入价格类型（收盘价、开盘价、典型价、DeMark 等）。|
| `TrendMethod` | 阶梯平滑器的均线类型（Jurik、SMA、EMA 等）。|
| `StartLength` | 阶梯平滑器的初始周期。|
| `StepSize` | 每个阶梯在上一周期基础上增加的步长。|
| `StepsTotal` | 阶梯平滑器的阶段数量。|
| `SmoothingMethod` | 对多空计数进行二次平滑时使用的均线类型。|
| `SmoothingLength` | 最终平滑的周期。|
| `UpLevelPercent` | 标记强势多头的百分比阈值。|
| `DownLevelPercent` | 标记强势空头的百分比阈值。|
| `SignalBar` | 用于发出信号的柱索引（1 表示前一根已完成的柱）。|
| `AllowBuyOpen` / `AllowSellOpen` | 是否允许开立多头/空头仓位。|
| `AllowBuyClose` / `AllowSellClose` | 是否允许平掉多头/空头仓位。|
| `StopLossOffset` | 止损距离（绝对价格，0 表示不下达止损）。|
| `TakeProfitOffset` | 止盈距离（绝对价格，0 表示不下达止盈）。|

## 使用说明

1. 将 `CandleType` 设置为期望的指标周期（原版默认使用 4 小时）。
2. 调整 `StepsTotal`、`StartLength` 和 `StepSize` 可以改变指标响应速度；阶段越多越平滑但滞后越大。
3. 将 `StopLossOffset` 和 `TakeProfitOffset` 设为 0 可禁用保护单。
4. 某些 MetaTrader 平滑方法在 StockSharp 中没有直接对应项，本策略使用 Jurik 或 EMA 作为替代。
5. 策略只在完成的 K 线上操作，以避免与原始版本出现偏差。
