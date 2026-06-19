# EMA/SMA + RSI 趋势交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略跟踪三条指数移动平均线（快线、中线和慢线）并配合 RSI 过滤器，以在新趋势中占据主动。当快线向上（或向下）穿越中线且价格位于慢线之上（或之下）并且蜡烛收盘方向一致时产生交易信号，从而减少震荡噪音。

可以设置在连续盈利的若干根 K 线后自动平仓。RSI 同时作为超买/超卖过滤器，当动量过度扩张时触发退出。

回测显示，该方法在趋势明确、移动平均线分离度高的加密货币品种上表现最佳。

## 详情

- **入场条件**:
  - **多头**: `EMA_fast > EMA_medium` 且 `EMA_fast(t-1) <= EMA_medium(t-1)` 且 `Close > EMA_slow` 且 `Close > Open`
  - **空头**: `EMA_fast < EMA_medium` 且 `EMA_fast(t-1) >= EMA_medium(t-1)` 且 `Close < EMA_slow` 且 `Close < Open`
- **多空方向**: 双向
- **退出条件**:
  - **多头**: `RSI > 70` 或 达到 `X` 根盈利 K 线且 `Close > entry`
  - **空头**: `RSI < 30` 或 达到 `X` 根盈利 K 线且 `Close < entry`
- **止损**: 无
- **默认值**:
  - `EMA_fast` = 10
  - `EMA_medium` = 20
  - `EMA_slow` = 100
  - `RSI_length` = 14
  - `X bars` = 24
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: EMA, RSI
  - 止损: 可选的时间离场
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
