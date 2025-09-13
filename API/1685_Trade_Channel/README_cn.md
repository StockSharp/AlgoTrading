
# Trade Channel 策略

**Trade Channel 策略** 在 Donchian 价格通道周围交易突破与回调。当上轨保持不变且价格触及上轨或收于其下方但高于枢轴点时开多；相反条件下开空。止损设置在相反通道边界之外并加上 ATR 值，可选的移动止损用于跟随盈利。

## 参数

- `ChannelPeriod` — Donchian 通道的长度。
- `AtrPeriod` — 计算止损的 ATR 周期。
- `Trailing` — 移动止损距离，价格单位（0 表示关闭）。
- `CandleType` — 用于计算的蜡烛类型。
