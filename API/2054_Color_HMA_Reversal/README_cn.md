# Color HMA Reversal
[English](README.md) | [Русский](README_ru.md)

基于 Hull 移动平均线斜率变化的策略。当 HMA 由下降转为上升时，策略平掉空单并开多；当 HMA 由上升转为下降时，策略平掉多单并开空。

## 参数
- `HmaPeriod` — HMA 的周期。
- `CandleType` — 使用的 K 线周期。
- `BuyOpen`, `SellOpen` — 允许开多/开空。
- `BuyClose`, `SellClose` — 允许平多/平空。

## 信号
- **向上反转**：之前 HMA 下降，当前值开始上升 → 平空并开多。
- **向下反转**：之前 HMA 上升，当前值开始下降 → 平多并开空。

策略使用市价单，并按 `Strategy.Volume` 中指定的数量交易。
