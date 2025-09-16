# Macd Cci Lotfy 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 MACD 与 CCI 指标，并使用系数对 MACD 进行缩放。
当两个指标在同一方向突破极值阈值时开仓。

MACD 值乘以系数后与 CCI 同一阈值比较，旨在捕捉超买和超卖后的反转。

## 细节

- **入场条件**：
  - 多头：`CCI < -Threshold` 且 `MACD * MacdCoefficient < -Threshold`
  - 空头：`CCI > Threshold` 且 `MACD * MacdCoefficient > Threshold`
- **方向**：双向
- **出场条件**：相反信号触发反向仓位
- **止损**：无
- **默认值**：
  - `CciPeriod` = 8
  - `FastPeriod` = 13
  - `SlowPeriod` = 33
  - `MacdCoefficient` = 86000
  - `Threshold` = 85
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 分类：均值回归
  - 方向：双向
  - 指标：MACD, CCI
  - 止损：否
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

