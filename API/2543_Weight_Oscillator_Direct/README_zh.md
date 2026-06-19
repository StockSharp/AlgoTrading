# Weight Oscillator Direct 策略

## 概述
该策略将 MetaTrader 专家顾问 **Exp_WeightOscillator_Direct** 移植到 StockSharp 的高级 API。它把 RSI、资金流量指数 (MFI)、威廉指标 (Williams %R) 和 DeMarker 四个振荡指标按权重合成为一个综合信号，并通过可选的移动平均进行平滑处理。当 *Trend Mode* 为 “Direct” 时，策略沿着综合振荡指标的斜率开仓；当设置为 “Against” 时，则执行反向交易。

## 指标流水线
1. **RSI**：输出范围 0..100。
2. **MFI**：同样归一化到 0..100，用于衡量成交量动能。
3. **Williams %R**：先平移 +100，使其落入 0..100 区间。
4. **DeMarker**：乘以 100，与其它振荡指标保持一致。
5. **平滑处理**：选择 Simple、Exponential、Smoothed (RMA)、Weighted、Jurik 或 Kaufman 自适应均线之一。
6. **综合振荡器**：对上述值按权重求平均并进行平滑，得到最终交易信号。

每根已完成的 K 线都会保存一次综合振荡器的数值。通过 *Signal Bar* 参数可以跳过最近的若干根 K 线，完全复刻原始 EA 的信号延迟逻辑。

## 交易逻辑
1. 等待所有基础指标和平滑均线形成。
2. 计算当前完成 K 线的综合振荡器并写入历史序列。
3. 取出 `Signal Bar`、`Signal Bar + 1`、`Signal Bar + 2` 对应的三个历史值，记为 `current`、`previous`、`prior`。
4. 判断斜率变化：
   - **上升**：`previous < prior` 且 `current > previous`。
   - **下降**：`previous > prior` 且 `current < previous`。
5. 根据 *Trend Mode*：
   - **Direct**：上升触发做多，下降触发做空。
   - **Against**：信号方向取反，上升做空，下降做多。
6. 处理进出场开关：
   - 若启用 *Close Shorts/Longs on Signal*，先平掉相反方向的持仓。
   - 若启用 *Allow Long/Short Entries*，再以 `Volume + |Position|` 的数量下市价单，从而在一次委托中完成反手。
7. 如果 *Stop Loss Points* 或 *Take Profit Points* 大于 0，则通过 `StartProtection` 以价格步长 (PriceStep) 为单位启用止损止盈。

## 参数说明
| 分组 | 名称 | 说明 |
|------|------|------|
| General | **Candle Type** | 指标计算和信号使用的 K 线周期。 |
| Trading | **Trend Mode** | `Direct` 顺势，`Against` 逆势。 |
| Trading | **Signal Bar** | 跳过的已完成 K 线数量（默认 1 表示上一根 K 线）。 |
| Oscillator | **RSI / MFI / WPR / DeMarker Weight** | 各振荡指标的权重，设为 0 可禁用该指标。 |
| Oscillator | **RSI / MFI / WPR / DeMarker Period** | 各指标的计算周期。 |
| Oscillator | **Smoothing Method** | 选择平滑均线类型（Simple、Exponential、Smoothed、Weighted、Jurik、Kaufman）。 |
| Oscillator | **Smoothing Length** | 平滑均线的周期长度。 |
| Risk Management | **Stop Loss Points** | 止损距离（价格步长），0 表示关闭。 |
| Risk Management | **Take Profit Points** | 止盈距离（价格步长），0 表示关闭。 |
| Trading | **Allow Long/Short Entries** | 是否允许开多 / 开空。 |
| Trading | **Close Shorts/Longs on Signal** | 是否在反向信号出现时强制平仓。 |

所有参数都以 `StrategyParam` 暴露，可在 StockSharp Designer 中进行优化。

## 使用提示
- 启动前请设定基础 `Volume`，策略在反手时会自动加上已有仓位的绝对值以一次成交完成换向。
- 仅订阅 `GetWorkingSecurities()` 返回的那一组蜡烛数据。
- 止损和止盈使用 `PriceStep` 转换为绝对价格，因此要确保品种的步长设置正确。
- “Against” 模式只改变信号方向，其余逻辑与原 EA 保持一致。
- Williams %R 与 DeMarker 在内部做了归一化，与 RSI、MFI 处于同一量纲。

## 与 MQL 版本的差异
- 原策略还提供 `ParMA`、`JurX`、`VIDYA`、`T3` 等平滑方式。StockSharp 版提供 Jurik 与 Kaufman 等高质量替代方案，并默认使用 Jurik。
- Money Flow Index 总是使用 K 线的成交量。在 MetaTrader 中可以指定 tick 量或真实成交量，而在 StockSharp 中取决于数据源。
- 资金管理通过 `StartProtection` 实现，以价格步长定义距离，更适合 StockSharp 生态，并能得到与原策略相同的效果。

## 快速开始
1. 将策略连接到目标投资组合与证券。
2. 设置各振荡器的权重、周期及进出场开关。
3. 按市场特性选择合适的平滑方法和周期。
4. 如需风险控制，配置止损 / 止盈步长。
5. 启动策略，所有交易均在 K 线收盘后执行，确保结果可复现。
