# TradingLab Best MACD 策略

**TradingLab Best MACD** 策略将 Mueller Peter 在 MetaTrader 上的专家顾问移植到 StockSharp 高阶 API。策略把 200 周期 EMA 趋势过滤、MACD 动量信号以及最近支撑/阻力触碰情况结合在一起，只在 K 线收盘后行动，并按照原始 MQL 算法为每笔交易计算动态止损与止盈。

## 核心逻辑

1. **指标组合**
   - 200 周期 EMA（使用所选的 `CandleType`）判定趋势方向。
   - 标准 MACD（12/26 与 9 信号线）提供多空交叉动量。
   - 两个通道指标追踪结构：长度 20 的 `Highest` 代表阻力，长度 10 的 `Lowest` 代表支撑。
2. **信号有效期计数器**
   - 如果上一根蜡烛的最高价突破阻力，`ResistanceTouch` 计数器被重置为 `SignalValidity` 并随每根新 K 线递减。
   - 如果最低价跌破支撑，`SupportTouch` 计数器同样被重置并递减。
   - MACD 在零线下方的金叉或零线上方的死叉会分别刷新多头或空头的 MACD 计数器，使动量信号在多根 K 线内保持有效。
3. **入场条件**
   - **多头**：收盘价高于 EMA、MACD 向上计数器与支撑触碰计数器都在有效期内，并且至少一个是在最新 K 线上刚触发。下单量等于 `OrderVolume` 加上平掉既有空头所需的数量。
   - **空头**：收盘价低于 EMA、MACD 向下计数器与阻力触碰计数器都有效，并且至少一个是最新触发。
4. **离场管理**
   - 入场时根据 EMA 与 `StopDistancePoints` 计算止损，按照价格步长转换为绝对价格。止盈沿用 MQL 公式：`(入场价 - EMA + 止损距离) * 1.5` 加到入场价（空头则减去）。
   - 每根收盘 K 线都会检测最高价/最低价是否触及这些水平并立即平仓。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `SignalValidity` | 结构或 MACD 信号保持有效的收盘 K 线数量。 | 7 |
| `OrderVolume` | 入场时使用的市场单手数。 | 1 |
| `StopDistancePoints` | 止损相对 EMA 的点数距离。 | 50 |
| `CandleType` | 提供数据的主要蜡烛类型（默认 5 分钟）。 | `TimeSpan.FromMinutes(5).TimeFrame()` |

所有参数都通过 `StrategyParam` 暴露，可在 Designer 中优化。

## 交易流程

1. 订阅所选蜡烛并绑定 EMA、MACD、Highest、Lowest 指标。
2. 维护前一根蜡烛的高低点与指标值，以复现 MQL 中 `CopyBuffer` 的回溯逻辑。
3. 在每根已收盘的 K 线上更新四个信号计数器。
4. 调用 `IsFormedAndOnlineAndAllowTrading()` 确认策略准备就绪后再提交订单。
5. 下达市价单并记录动态止损/止盈，在随后的每根 K 线上检查是否被触发。

## 说明

- 按要求仅提供 C# 版本，没有创建 Python 代码或 `PY/` 目录。
- 策略全部基于高阶 API (`SubscribeCandles`, `BindEx`, `BuyMarket`, `SellMarket`) 实现，无需手动维护指标缓冲区。
- 如果存在图表区域，会自动绘制蜡烛、EMA 与 MACD，方便验证信号。
