# Bollinger EMA Stats 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用两组布林带来定义入场和止损区域，并使用EMA作为退出目标。

## 细节
- **入场条件**：
  - **多头**：收盘价低于布林带下轨（入场倍数）。
  - **空头**：收盘价高于布林带上轨（入场倍数）。
- **多空方向**：双向。
- **出场条件**：
  - 价格到达EMA获利。
  - 在更宽的布林带处止损。
- **止损**：是。
- **默认值**：
  - `BB Length` = 20
  - `Entry StdDev Mult` = 2.0
  - `Stop StdDev Mult` = 3.0
  - `EMA Exit Period` = 20
- **过滤器**：
  - 分类：波动率
  - 方向：双向
  - 指标：Bollinger Bands, EMA
  - 止损：是
  - 复杂度：低
  - 时间框架：中期
