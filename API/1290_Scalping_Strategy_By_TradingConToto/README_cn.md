# Scalping Strategy By TradingConToto
[English](README.md) | [Русский](README_ru.md)

Scalping Strategy By TradingConToto 根据 EMA 趋势在连续的枢轴高点或低点之间绘制连线。当上升趋势中价格向上突破下降的枢轴高点连线时做多；当下降趋势中价格向下跌破上升的枢轴低点连线时做空。策略仅在指定的交易时段内执行。

## 细节

- **入场条件**：上升趋势中价格突破下降的枢轴高点连线做多；下降趋势中价格跌破上升的枢轴低点连线做空。
- **多空方向**：双向。
- **出场条件**：止盈和止损。
- **止损**：是。
- **默认值**：
  - `Pivot` = 16
  - `Pips` = 64
  - `Spread` = 0
  - `Session` = "0830-0930"
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选条件**：
  - 分类：突破
  - 方向：双向
  - 指标：EMA、枢轴
  - 止损：有
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险水平：中等
