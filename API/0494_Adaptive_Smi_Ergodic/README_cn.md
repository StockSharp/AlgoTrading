# Adaptive SMI Ergodic策略
[English](README.md) | [Русский](README_ru.md)

Adaptive SMI Ergodic策略利用True Strength Index (TSI)振荡器及其EMA信号线来识别超买或超卖后的反转。当TSI上穿超卖阈值并位于信号线上方时开多仓；当TSI下穿超买阈值且位于信号线下方时开空仓。

## 细节

- **入场条件**：
  - TSI上穿超卖阈值且TSI > 信号线（多）。
  - TSI下穿超买阈值且TSI < 信号线（空）。
- **方向**：多头和空头。
- **出场条件**：
  - 反向信号触发反向交易。
- **止损**：无。
- **默认参数**：
  - `LongLength` = 12
  - `ShortLength` = 5
  - `SignalLength` = 5
  - `OversoldThreshold` = -0.4
  - `OverboughtThreshold` = 0.4
- **过滤器**：
  - 类型：动量振荡器
  - 方向：多/空
  - 指标：True Strength Index、EMA
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：低
