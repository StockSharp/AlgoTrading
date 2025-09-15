# WPR Level Cross 策略

该策略基于 Williams %R 指标，当指标突破预设的超买和超卖水平时进行交易。

当指标跌破 **Low Level** 时，表示可能从超卖区域反转；当指标升破 **High Level** 时，表示可能从超买区域反转。通过参数 **Trend** 可以选择顺势交易（`Direct`）或反向交易（`Against`）。

## 参数

- `WprPeriod` – Williams %R 的计算周期。
- `HighLevel` – 超买阈值。
- `LowLevel` – 超卖阈值。
- `Trend` – 交易模式：`Direct` 按指标信号交易，`Against` 反向交易。
- `EnableBuyEntry` / `EnableSellEntry` – 允许开多/开空。
- `EnableBuyExit` / `EnableSellExit` – 允许平空/平多。
- `StopLoss` – 以价格单位表示的止损值。
- `TakeProfit` – 以价格单位表示的止盈值。
- `CandleType` – 计算所用的K线周期。

## 工作原理

1. 策略订阅K线并计算 Williams %R 指标。
2. 在每根完成的K线上检查指标是否突破设定水平。
3. 根据 `Trend` 和允许的操作，使用市价单开仓或平仓。
4. 通过 `StartProtection` 启用可选的止损和止盈保护。

## 说明

- 代码中的注释为英文。
- 仅提供 C# 版本，暂不包含 Python 版本。
