# FlexiMA Variance Tracker 策略
[English](README.md) | [Русский](README_ru.md)

跟踪价格相对于移动平均线的偏差，当偏差超过波动阈值且 SuperTrend 方向确认时开仓。

## 细节

- **入场条件**：
  - 价格高于 SuperTrend 且偏差 > 平均值 + 标准差 × 系数 → 做多。
  - 价格低于 SuperTrend 且偏差 < -(平均值 + 标准差 × 系数) → 做空。
- **多/空**：支持双向交易。
- **出场条件**：
  - 相反偏差或 SuperTrend 反转。
- **止损**：默认无止损逻辑。
- **默认参数**：
  - MA 长度 = 20。
  - StdDev 长度 = 20。
  - StdDev 系数 = 1.0。
  - ATR 周期 = 10。
  - ATR 系数 = 3.0。
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA、StandardDeviation、SuperTrend
  - 止损：无
  - 复杂度：中
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
