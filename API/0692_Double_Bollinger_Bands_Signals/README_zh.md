# Double Bollinger Bands Signals 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用两组布林带。当价格上穿下方 3 倍标准差带时买入，当价格下穿上方 3 倍标准差带时卖出。仓位在相反的 2 倍标准差带处平仓。

## 细节

- **入场条件**：
  - 多头：收盘价上穿下方 3 SD 带
  - 空头：收盘价下穿上方 3 SD 带
- **方向**：双向
- **出场条件**：
  - 多头：收盘价上穿上方 2 SD 带
  - 空头：收盘价下穿下方 2 SD 带
- **止损**：无
- **默认值**：
  - `Length` = 20
  - `Width1` = 2m
  - `Width2` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：均值回归
  - 方向：双向
  - 指标：Bollinger Bands
  - 止损：无
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
