# AI Volume策略
[English](README.md) | [Русский](README_ru.md)

AI Volume策略寻找成交量的突然放大。当当前成交量超过其EMA乘以设定的倍数，价格位于50期EMA之上/下并且K线颜色一致时，在该方向开仓。每笔交易在固定数量的K线后平仓。

## 细节

- **入场条件**：成交量 > VolumeEMA × VolumeMultiplier 且价格与K线颜色相符地位于50 EMA之上/下。
- **方向**：双向。
- **出场条件**：在 `ExitBars` 根K线后平仓。
- **止损**：无。
- **默认参数**：
  - `VolumeEmaLength` = 20
  - `VolumeMultiplier` = 2.0
  - `ExitBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类型：成交量突破
  - 方向：双向
  - 指标：EMA、Volume EMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

