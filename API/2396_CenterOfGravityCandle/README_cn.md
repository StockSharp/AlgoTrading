# 重心蜡烛策略

该策略使用 StockSharp 高级 API 复刻 MetaTrader 专家顾问 “Exp_CenterOfGravityCandle”。指标对原始蜡烛的开、高、低、收价格分别应用 John Ehlers 的重心算法，并通过可配置的移动平均线进行平滑，构建出三色的合成蜡烛。蜡烛颜色（多头、空头或中性）即为唯一的交易信号。

## 指标原理

1. 仅在市场蜡烛完全收盘后才参与计算。
2. 对每个价格分量（开盘价、高点、低点、收盘价）分别计算周期为 **Period** 的简单移动平均线和线性加权移动平均线。
3. 将两条均线的乘积除以品种的最小跳动点，并按 **Ma Method** 和 **Smooth Period** 指定的方式进行二次平滑。
4. 为保持与 MetaTrader 指标一致，会强制让合成最高价与最低价覆盖合成开收盘价，从而保证蜡烛实体完整。
5. 合成开盘价低于合成收盘价视为多头蜡烛（颜色 2），高于收盘价视为空头蜡烛（颜色 0），两者相等为中性蜡烛（颜色 1）。

## 交易规则

1. 策略维护最近若干根合成蜡烛的颜色，并观察由 **Signal Bar** 指定的那一根（默认上一根已完成的蜡烛）。
2. 当观测蜡烛从非多头变为多头时：
   - 若 **Enable Sell Close** 为 `true`，关闭当前空头仓位。
   - 若 **Enable Buy Open** 为 `true`，按计算手数开多仓。
3. 当观测蜡烛从非空头变为空头时：
   - 若 **Enable Buy Close** 为 `true`，关闭当前多头仓位。
   - 若 **Enable Sell Open** 为 `true`，按计算手数开空仓。
4. 入场手数依据 **Money Management** 与 **Margin Mode** 计算。Money Management 为负值时视为固定手数。选择亏损基准模式时，会结合 **Stop Loss** 距离估算单次风险。
5. 调用 `StartProtection`，根据 **Stop Loss** 与 **Take Profit**（以价格跳动计算）自动挂出止损/止盈单。

## 参数

- **Money Management** – 决定下单手数的资金占比，负值代表直接指定手数，可参与优化。
- **Margin Mode** – 对资金占比的解释方式（权益、余额、亏损风险或固定手数）。
- **Stop Loss** – 止损距离（按价格跳动），用于仓位保护与风险计算。
- **Take Profit** – 止盈距离（按价格跳动），由 `StartProtection` 执行。
- **Open Long / Open Short** – 是否允许在信号出现时开多/开空。
- **Close Long / Close Short** – 是否允许在反向信号出现时平多/平空。
- **Candle Type** – 用于计算指标的蜡烛周期。
- **Center of Gravity Period** – 简单与线性加权均线的基础周期，可优化。
- **Smoothing Period** – 合成蜡烛平滑阶段的周期，可优化。
- **Smoothing Method** – 平滑所用的移动平均类型（SMA、EMA、SMMA、LWMA）。
- **Signal Bar** – 用于产生信号的合成蜡烛索引（0 = 当前，1 = 上一根）。

## 说明

- 指标在 C# 中完整实现，复刻 MetaTrader 算法，无需额外的历史缓冲区。
- 手数计算依赖 StockSharp 的投资组合数据，与 MetaTrader 的结果可能存在细微差异。
- 策略仅处理完全收盘的蜡烛，不在未完成的柱子上交易。
