# RoNz Rapid-Fire 策略

该策略结合移动平均线和抛物线SAR指标，用于捕捉快速趋势变化。当收盘价上穿移动平均线且抛物线SAR切换到价格下方时开多仓；当收盘价下穿移动平均线且抛物线SAR切换到价格上方时开空仓。趋势持续时可以选择加仓。

## 工作原理
- **多头入场**：收盘价 > SMA 且抛物线SAR位于价格下方。
- **空头入场**：收盘价 < SMA 且抛物线SAR位于价格上方。
- **平仓**：根据选择的模式，通过止损/止盈或反向信号平仓。
- **加仓**：趋势延续时追加仓位。
- **移动止损**：随着盈利移动调整止损价位。

## 参数
- `Volume` – 交易量。
- `StopLoss` – 止损（点）。
- `TakeProfit` – 止盈（点）。
- `TrailingStop` – 移动止损（点）。
- `Averaging` – 是否启用加仓。
- `MaPeriod` – 移动平均周期。
- `PsarStep` – 抛物线SAR步长。
- `PsarMax` – 抛物线SAR最大值。
- `CloseType` – `SlClose` 仅使用止损/止盈，`TrendClose` 依据反向趋势平仓。
- `CandleType` – 计算所用的K线类型。

## 备注
- 适用于 StockSharp 支持的任何品种。
- 需要为所选 `CandleType` 提供历史K线数据。
