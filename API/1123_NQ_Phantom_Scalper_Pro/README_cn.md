# NQ Phantom Scalper Pro 策略
[English](README.md) | [Русский](README_ru.md)

基于 VWAP 带的突破策略，可选成交量和趋势过滤。

## 详情

- **入场条件**：
  - **多头**：价格收于上方 VWAP 带之上且成交量放大。
  - **空头**：价格收于下方 VWAP 带之下且成交量放大。
- **多空**：双向
- **出场条件**：
  - 价格回到 VWAP 或触发 ATR 止损。
- **止损**：基于 ATR
- **默认值**：
  - `Band #1 Mult` = 1.0
  - `Band #2 Mult` = 2.0
  - `ATR Length` = 14
  - `ATR Stop Mult` = 1.0
  - `Volume SMA Period` = 20
  - `Volume Spike Mult` = 1.5
  - `Trend EMA Length` = 50
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：VWAP、ATR、EMA、SMA
  - 止损：有
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
