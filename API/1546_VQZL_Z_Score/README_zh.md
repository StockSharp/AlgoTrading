# VQZL Z-Score
[English](README.md) | [Русский](README_ru.md)

该策略基于价格相对平滑均值的Z分数。

测试显示年化收益约42%，在股票市场表现最好。

策略计算平滑移动平均线和标准差得到Z分数。当价格超出阈值时，按走势方向入场。

## 详情

- **入场条件**：
  - **做多**：`Z-Score > threshold`。
  - **做空**：`Z-Score < -threshold`。
- **多空方向**：双向。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `PriceSmoothing` = 15
  - `ZLength` = 100
  - `Threshold` = 1.64
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤条件**：
  - 类型：趋势
  - 方向：双向
  - 指标：SMA, StandardDeviation
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
