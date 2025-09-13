# 自适应 Renko 策略

该策略构建一个自适应的 Renko 网格，其砖块大小根据 **ATR（平均真实波幅）** 指标衡量的市场波动性进行调整。当价格在任意方向移动一个完整砖块时执行交易。

## 逻辑
- ATR 根据可配置的 `VolatilityPeriod` 计算。
- 砖块大小等于 `ATR * Multiplier`，但不会小于 `MinBrickSize`。
- 当价格向上突破前一个砖块至少一个砖块大小时，策略买入（如有需要，先平空仓）。
- 当价格向下突破前一个砖块至少一个砖块大小时，策略卖出（如有需要，先平多仓）。

## 参数
- `Volume` – 订单数量。
- `VolatilityPeriod` – ATR 的计算周期。
- `Multiplier` – ATR 的乘数。
- `MinBrickSize` – 砖块的最小尺寸（价格单位）。
- `CandleType` – 用于 ATR 计算的时间框架。

## 时间框架
- 默认：4 小时。
