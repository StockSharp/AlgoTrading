# Kijun Sen Robot 策略

## 概述
**Kijun Sen Robot 策略** 是将 MetaTrader 5 专家顾问 "Kijun Sen Robot" 迁移到 StockSharp 高级策略 API 的版本。默认使用 30 分钟 K 线，通过观察价格突破 Ichimoku 基准线（Kijun-sen）并结合 20 周期线性加权均线（LWMA）确认趋势来执行交易。策略保留了原始 EA 仅在活跃交易时段操作、并使用动态止损、保本和移动止损保护仓位的理念。

## 指标与数据
- **Ichimoku**：Tenkan/Kijun/Senkou Span B 默认周期为 6/12/24。
- **线性加权移动平均线（LWMA）**：20 根 K 线，用于确认趋势方向及与 Kijun 的距离。
- **K 线数据**：默认使用 30 分钟周期，可通过 `CandleType` 参数切换其他时间框架。

## 交易逻辑
### 多头入场
1. 当根 K 线从下方穿越 Kijun 线（开盘在下方、收盘在上方或盘中触及），且上一根 K 线收盘也位于 Kijun 下方。
2. 当前 Kijun 相比两根之前持平或抬升。
3. LWMA 至少低于 Kijun `MaFilterPips`（按品种最小价位换算）的距离。
4. LWMA 斜率为正，即当前值高于上一根的值。
5. 当前时间位于 `TradingStartHour` 与 `TradingEndHour` 之间（默认 07:00–19:00）。

满足条件并且当前净头寸 ≤ 0 时，策略会以市价买入（若存在空头会先回补）。入场价格取当前 K 线收盘价。

### 空头入场
1. 当前 K 线自上向下穿越 Kijun（逻辑与多头相反）。
2. Kijun 相比两根之前持平或下移。
3. LWMA 至少高于 Kijun `MaFilterPips` 的距离。
4. LWMA 斜率为负，即当前值低于上一根的值。
5. 仅在允许时间窗口内触发。

符合条件且净头寸 ≥ 0 时，策略以市价卖出（若存在多头会先平仓）。

### 仓位管理与退出
- **初始止损**：在入场价下方（做多）或上方（做空）放置 `StopLossPips` 的距离，按照品种 `PriceStep` 换算为价格，复刻原 EA 的保护性止损。
- **保本移动**：当浮盈达到 `BreakEvenPips` 后，将止损上调到入场价上方一个点（做多）或入场价下方一个点（做空）。
- **移动止损**：浮盈达到 `TrailingStopPips` 时，止损随价格按照该距离移动，仅朝有利方向调整。
- **固定止盈**：`TakeProfitPips` 指定的盈利目标，设为 0 可关闭。
- **Kijun 斜率退出**：若 LWMA 在止损仍低于入场价前反向转折，则立即平仓，保留原 EA 的防御逻辑。
- **时间过滤**：允许时段外不再开新仓，但已有仓位仍会持续执行上述保护规则。
- **订单类型**：全部使用市价单；原策略根据报价选择挂单或市价的细节被简化，因为 StockSharp 使用 K 线数据而非逐笔行情。

若同一根 K 线同时触及止损与止盈，策略优先触发止损，以便在无盘中信息时采取保守处理。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TenkanPeriod` | 6 | Ichimoku Tenkan 周期。 |
| `KijunPeriod` | 12 | Ichimoku Kijun 周期。 |
| `SenkouSpanBPeriod` | 24 | Ichimoku Senkou Span B 周期。 |
| `LwmaPeriod` | 20 | LWMA 趋势确认周期。 |
| `MaFilterPips` | 6 | Kijun 与 LWMA 最小距离（单位：点）。 |
| `StopLossPips` | 50 | 初始保护性止损。 |
| `BreakEvenPips` | 9 | 触发保本的浮盈距离。 |
| `TrailingStopPips` | 10 | 移动止损距离。 |
| `TakeProfitPips` | 120 | 固定止盈距离（0 = 关闭）。 |
| `TradingStartHour` | 7 | 允许交易起始小时（包含）。 |
| `TradingEndHour` | 19 | 允许交易结束小时（不包含）。 |
| `CandleType` | 30 分钟 | 信号使用的 K 线类型。 |

所有以点数表示的参数均根据品种的 `PriceStep` 转换成价格单位。当品种最小报价精度为 3 或 5 位小数时，会自动乘以 10，以模拟 MT5 中 `digits_adjust` 的处理方式。

## 实现说明
- `_pendingLongLevel` 与 `_pendingShortLevel` 用于模拟原 EA 中的 `longcross/shortcross` 状态变量，保证每次开仓都必须等待新的 Kijun 穿越。
- MT5 中基于最后买/卖价的判断改为使用 K 线的开、高、低、收，适合在 StockSharp 回测环境下的确定性逻辑。
- 止损、保本和移动止损由策略内部跟踪并通过 `ClosePosition()` 执行，而不是修改服务器端挂单。
- `ConvertPips` 使用 `Security.PriceStep` 或 `Security.MinPriceStep` 并结合 3/5 位小数的 10 倍调整，复刻原策略的点值换算。
- 通过 `SubscribeCandles().BindEx(...)` 绑定 Ichimoku 与 LWMA 指标，同时在图表区域绘制 K 线、指标与自有成交。

## 使用建议
1. 选择支持 30 分钟 K 线的交易品种，可按需修改 `CandleType`。
2. 启动前设置策略的 `Volume` 属性为期望下单手数。
3. 根据品种波动或已有优化结果调整相关点数参数。
4. 在高阶回测或实时环境中运行，策略会自动执行时间过滤与仓位保护逻辑。
5. 通过日志或图表观察保本/移动止损的触发情况。按照要求，代码中的注释全部使用英文。

本文件夹仅包含 C# 版本，未提供 Python 实现。
