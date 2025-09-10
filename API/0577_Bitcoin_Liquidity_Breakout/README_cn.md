# 比特币流动性突破策略
[English](README.md) | [Русский](README_ru.md)

该策略在流动性和波动性较高且短期趋势看涨时建立多头仓位。当成交量高于其移动平均值乘以阈值时视为高流动性；当ATR高于其移动平均值时确认波动性。

## 详情

- **入场条件**:
  - `成交量 > 成交量SMA * LiquidityThreshold`
  - `价格变动(%) > PriceChangeThreshold`
  - `快SMA > 慢SMA`
  - `RSI < 65`
  - `ATR > SMA(ATR,10)`
- **多空方向**: 仅做多。
- **出场条件**: 快SMA下穿慢SMA或RSI > 70。
- **止损**: 可选的止损和止盈百分比。
- **默认参数**:
  - `LiquidityThreshold` = 1.3
  - `PriceChangeThreshold` = 1.5
  - `VolatilityPeriod` = 14
  - `LiquidityPeriod` = 20
  - `FastMaPeriod` = 9
  - `SlowMaPeriod` = 21
  - `RsiPeriod` = 14
  - `StopLossPercent` = 0.5
  - `TakeProfitPercent` = 7
- **过滤器**:
  - 分类: 突破
  - 方向: 多头
  - 指标: SMA, RSI, ATR
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 1小时
