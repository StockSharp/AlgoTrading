# Flex ATR
[English](README.md) | [Русский](README_ru.md)

Flex ATR 根据当前时间框架动态选择 EMA、RSI 和 ATR 周期。当快速 EMA 上穿慢速 EMA 且 RSI 高于 50 时做多；当快速 EMA 下穿慢速 EMA 且 RSI 低于 50 时做空。退出基于 ATR 的止损或止盈，并可选启用追踪止损。

## 详情

- **入场条件**：快慢 EMA 交叉并结合 RSI 过滤。
- **多空方向**：双向。
- **退出条件**：ATR 止损或止盈，可选追踪止损。
- **止损**：支持。
- **默认值**：
  - `AtrStopMult` = 3
  - `AtrProfitMult` = 1.5
  - `EnableTrailingStop` = true
  - `AtrTrailMult` = 1
  - `CandleType` = TimeSpan.FromMinutes(30)
- **筛选器**：
  - 分类: Trend
  - 方向: Both
  - 指标: EMA, RSI, ATR
  - 止损: Yes
  - 复杂度: Advanced
  - 时间框架: Any
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
