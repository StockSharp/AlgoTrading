# FlexiSuperTrend 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 SuperTrend 滤波与平滑的偏差振荡器。
当价格与 SuperTrend 方向一致且振荡器确认动量时开仓。

## 细节

- **入场条件**：
  - 价格高于 SuperTrend 且偏差（价格与 SuperTrend 之差的 SMA）> 0 → 做多。
  - 价格低于 SuperTrend 且偏差 < 0 → 做空。
- **多/空**：支持双向交易。
- **出场条件**：
  - 当价格穿越 SuperTrend 线时趋势反转。
- **止损**：默认无止损逻辑。
- **默认参数**：
  - ATR 周期 = 10。
  - ATR 系数 = 3.0。
  - SMA 长度 = 10。
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SuperTrend、SMA
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
