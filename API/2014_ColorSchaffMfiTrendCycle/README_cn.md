# Color Schaff MFI Trend Cycle 策略

该策略翻译自 MQL5 专家 `Exp_ColorSchaffMFITrendCycle`。
它使用 **Color Schaff MFI Trend Cycle** 指标，将快速和慢速 MFI
经过双重随机计算得到趋势值，并以八种颜色表示趋势强弱。

交易规则：

- 前一根柱的颜色为绿色（索引 6-7）且当前颜色低于强势上升区时，
  平掉空头并开多。
- 前一根柱的颜色为橙色（索引 0-1）且当前颜色高于强势下降区时，
  平掉多头并开空。

参数：

- `FastMfiPeriod` – 快速 MFI 周期。
- `SlowMfiPeriod` – 慢速 MFI 周期。
- `CycleLength` – 指标内部循环长度。
- `HighLevel` / `LowLevel` – 超买和超卖阈值。
- `CandleType` – K 线时间框架，默认 1 小时。

策略仅处理完成的 K 线，并使用 StockSharp 的高级 API。
