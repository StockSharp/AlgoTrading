# Adaptive Trend Flow策略
[English](README.md) | [Русский](README_ru.md)

Adaptive Trend Flow策略基于典型价格的快慢EMA构建波动率通道。当价格突破通道边界时，内部趋势发生反转。趋势向上并且可选的SMA和MACD过滤条件满足时开多仓；当趋势转向下方时平掉仓位。

## 细节

- **入场条件**：
  - 趋势从下行转为上行且过滤器确认。
- **方向**：仅做多。
- **出场条件**：
  - 趋势从上行转为下行。
- **止损**：无。
- **默认参数**：
  - `Length` = 2
  - `SmoothLength` = 2
  - `Sensitivity` = 2.0
  - `UseSmaFilter` = true
  - `SmaLength` = 4
  - `UseMacdFilter` = true
  - `MacdFastLength` = 2
  - `MacdSlowLength` = 7
  - `MacdSignalLength` = 2
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多头
  - 指标：EMA、SMA、MACD、Standard Deviation
  - 止损：无
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
