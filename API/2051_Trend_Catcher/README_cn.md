# Trend Catcher 策略
[English](README.md) | [Русский](README_ru.md)

**Trend Catcher** 策略将 Parabolic SAR 与多条简单移动平均线结合，用于捕捉趋势走势。当价格在快均线方向上穿越 Parabolic SAR 时入场，并通过动态止损和跟踪止损管理仓位。

当最新 K 线收盘价位于上一根 K 线相反的 SAR 一侧，且快速均线确认方向时触发交易。初始止损基于价格与 SAR 点的距离，并限制在最小和最大范围内。止盈设置为止损距离的倍数。价格前进到指定距离后，止损移动到保本位置并继续跟踪价格。

## 细节

- **入场条件**:
  - **多头**: `Close[0] > SAR && Close[1] < SAR_prev && FastMA > SlowMA && Close > FastMA2`。
  - **空头**: `Close[0] < SAR && Close[1] > SAR_prev && FastMA < SlowMA && Close < FastMA2`。
- **出场条件**:
  - 触发止损或止盈。
  - 达到利润阈值后启动跟踪止损。
  - 反向信号关闭现有头寸。
- **止损**: 基于 SAR 的动态止损，支持保本和跟踪。
- **默认值**:
  - `SlowMaPeriod = 200`
  - `FastMaPeriod = 50`
  - `FastMa2Period = 25`
  - `SarStep = 0.004`
  - `SarMax = 0.2`
  - `SlMultiplier = 1`
  - `TpMultiplier = 1`
  - `MinStopLoss = 10`
  - `MaxStopLoss = 200`
  - `ProfitLevel = 500`
  - `BreakevenOffset = 1`
  - `TrailingThreshold = 500`
  - `TrailingDistance = 10`
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: Parabolic SAR, SMA
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
