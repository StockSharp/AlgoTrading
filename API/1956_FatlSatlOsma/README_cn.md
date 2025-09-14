# FatlSatlOsma 策略
[English](README.md) | [Русский](README_ru.md)

该示例在 StockSharp 高级 API 中复现了 MetaTrader 专家 **Exp_FatlSatlOsma** 的逻辑。  
原始系统使用 Fatl/Satl 振荡器（类似 MACD）。

- 当振荡器连续两根柱上升并且最新值高于前值时，开多并平空。
- 当振荡器连续两根柱下降并且最新值低于前值时，开空并平多。

振荡器通过内置的 `MovingAverageConvergenceDivergenceSignal` 指标实现。  
默认参数对应原始的 FATL/SATL 设置。

## 细节

- **入场条件**：振荡器加速。
- **方向**：双向。
- **出场条件**：反向加速。
- **止损**：无。
- **默认值**：
  - `Fast` = 39
  - `Slow` = 65
  - `CandleType` = 12 小时时间框
- **过滤器**：
  - 类别: 动量
  - 方向: 双向
  - 指标: MACD
  - 止损: 无
  - 复杂度: 基础
  - 周期: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等
