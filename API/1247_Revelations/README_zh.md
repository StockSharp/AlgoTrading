# Revelations 策略
[English](README.md) | [Русский](README_ru.md)

一种波动性突破策略，在 ATR 强烈尖峰且得到本地极值和状态指数确认时入场。头寸大小根据尖峰强度自适应。

## 详情

- **入场条件**：
  - **多头**：ATR 向上尖峰并在本地低点，且状态指数确认。
  - **空头**：ATR 向下尖峰并在本地高点，且状态指数确认。
- **多空方向**：双向。
- **出场条件**：
  - 触发止盈或止损。
- **止损**：固定百分比。
- **默认参数**：
  - `ATR Fast` = 14
  - `ATR Slow` = 21
  - `ATR StdDev` = 12
  - `Spike Threshold` = 0.5
  - `Super Spike Mult` = 1.5
  - `Regime Window` = 8
  - `Regime Events` = 3
  - `Local Window` = 3
  - `Max Quantity` = 2
  - `Min Quantity` = 1
  - `Stop %` = 0.9
  - `Take Profit %` = 1.8
- **过滤器**：
  - 分类：波动性突破
  - 方向：多/空
  - 指标：ATR, SMA, Highest/Lowest
  - 止损：是
  - 复杂度：高级
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
