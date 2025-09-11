# 动态波动率差模型
[English](README.md) | [Русский](README_ru.md)

**Dynamic Volatility Differential Model (DVDM)** 策略比较隐含波动率与历史波动率。当隐含波动率高于历史波动率并超过动态标准差阈值时做多，低于负阈值时做空。

信号基于日线数据，默认不设置止损。

## 详细信息
- **入场条件**: 波动率差超过上下动态标准差阈值。
- **多空**: 双向。
- **出场条件**: 波动率差穿越零轴。
- **止损**: 无。
- **默认值**:
  - `Length = 5`
  - `StdevMultiplier = 7.1m`
  - `VolatilitySecurity = "TVC:VIX"`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **过滤器**:
  - 分类: Volatility
  - 方向: Both
  - 指标: StandardDeviation
  - 止损: No
  - 复杂度: Intermediate
  - 时间框架: Daily
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
