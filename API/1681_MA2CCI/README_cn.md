# MA2CCI 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合快慢简单移动平均线 (SMA) 的交叉与商品通道指数 (CCI) 作为确认过滤。只有当均线与 CCI 同时在同一方向上越过各自的水平时才会开仓。平均真实波幅 (ATR) 用于确定入场时的止损距离。

策略可做多做空，不设定固定的止盈。平仓条件是出现相反信号或触发基于 ATR 的止损。

## 细节

- **入场条件**：
  - **多头**：快 SMA 上穿慢 SMA 且 CCI 上穿 0。
  - **空头**：快 SMA 下穿慢 SMA 且 CCI 下穿 0。
- **出场条件**：
  - 相反的 SMA 交叉。
  - 基于 ATR 的止损。
- **指标**：SMA、CCI、ATR。
- **时间框架**：通过 `CandleType` 参数设置。
- **默认参数**：
  - `Fast MA Period` = 4
  - `Slow MA Period` = 8
  - `CCI Period` = 4
  - `ATR Period` = 4
- **方向**：多/空。
- **止损**：使用 ATR 的动态止损。
