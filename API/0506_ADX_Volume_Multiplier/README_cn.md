# ADX Volume Multiplier 策略
[English](README.md) | [Русский](README_ru.md)

ADX Volume Multiplier 策略将平均方向性指数的趋势强度与成交量放大过滤器结合。只有当 ADX 超过阈值、主要方向线指向趋势方向且当前成交量超过移动平均乘以系数时才开仓。

## 细节

- **入场条件**：
  - ADX 高于阈值且 DI+ > DI-，并且成交量大于 SMA(成交量) * 系数 → 做多。
  - ADX 高于阈值且 DI- > DI+，并且成交量大于 SMA(成交量) * 系数 → 做空。
- **多空方向**：双向。
- **出场条件**：
  - 反向信号触发头寸反转。
- **止损**：无。
- **默认值**：
  - `AdxPeriod` = 21
  - `AdxThreshold` = 26
  - `VolumeMultiplier` = 1.8
  - `VolumePeriod` = 20
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：ADX、成交量 SMA
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中等
