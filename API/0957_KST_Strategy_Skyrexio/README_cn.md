# KST 策略 Skyrexio

当 Know Sure Thing (KST) 指标上穿其信号线且价格高于所选均线与鳄鱼指标的下颚线时，本策略做多。震荡指数过滤器可避免在盘整市况下入场。仓位通过基于 ATR 的止损和止盈退出。

- **入场条件**：KST 上穿信号线，价格高于过滤均线和鳄鱼下颚，震荡指数低于阈值。
- **出场条件**：价格触及 ATR 止损或 ATR 止盈。
- **指标**：KST、ATR、移动平均、鳄鱼下颚、震荡指数。

## 参数
- `CandleType` – K 线周期。
- `AtrStopLoss` – ATR 止损倍数。
- `AtrTakeProfit` – ATR 止盈倍数。
- `FilterMaType` – 过滤均线类型。
- `FilterMaLength` – 过滤均线周期。
- `EnableChopFilter` – 启用震荡指数过滤。
- `ChopThreshold` – 震荡指数阈值。
- `ChopLength` – 震荡指数周期。
- `RocLen1..4` – KST 的 ROC 周期。
- `SmaLen1..4` – KST 的 SMA 周期。
- `SignalLength` – KST 信号线周期。
