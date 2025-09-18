# BandOsMa 策略

## 概述
**BandOsMa 策略** 将 MetaTrader 5 的 “BandOsMA” 智能交易系统迁移到 StockSharp 平台。策略在 MACD 直方图（OsMA）上构建布林带，用来识别极值突破；直方图再经过一条可调移动平均线过滤，帮助判断信号是否仍然有效。

策略针对单一品种和时间框架运行，所需的指标值全部基于完成的蜡烛线，通过 StockSharp 的高级订阅接口计算。

## 交易逻辑
1. **指标**
   - `MovingAverageConvergenceDivergenceSignal` 输出 MACD 直方图（OsMA）。
   - `BollingerBands` 直接作用于 OsMA 序列，用于探测极端偏离。
   - 一条自定义的移动平均线平滑 OsMA，用作退出过滤器。
2. **进场**
   - 当当前 OsMA 收于下轨之下而前一根柱子仍在下轨之上时触发做多信号。
   - 当当前 OsMA 收于上轨之上而前一根柱子仍在上轨之下时触发做空信号。
3. **离场**
   - 当直方图与移动平均线出现反向交叉时信号失效。
   - 持仓方向与信号不一致时，立即平仓。
   - 每笔交易附带基于点值的止损，并使用与止损相同的距离做追踪止损，其追踪步长等于 `StopLossPoints / 50`，与原 MQL 示例一致。

## 仓位管理
- **止损与追踪**：止损距离以 MetaTrader 点值输入，并用 `PriceStep` 转换成价格单位。同一距离用于追踪止损，只有当收盘价较上一次止损位置至少改善一个追踪步长时才会移动。
- **单一持仓**：策略始终保持单向净头寸；出现反向信号时先平掉当前仓位，再考虑新的方向。

## 参数
| 分组 | 名称 | 说明 | 默认值 |
| --- | --- | --- | --- |
| General | `CandleType` | 指标计算使用的时间框架。 | `H1` |
| Risk | `LotSize` | 交易手数。 | `0.01` |
| Risk | `StopLossPoints` | 止损距离（点值，亦用于追踪止损）。 | `1000` |
| Indicators | `MacdFastPeriod` | MACD 快速 EMA 周期。 | `12` |
| Indicators | `MacdSlowPeriod` | MACD 慢速 EMA 周期。 | `26` |
| Indicators | `MacdSignalPeriod` | MACD 信号线 EMA 周期。 | `9` |
| Indicators | `PriceType` | MACD 所使用的价格类型（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 | `Typical` |
| Indicators | `BollingerPeriod` | OsMA 序列上的布林带周期。 | `26` |
| Indicators | `BollingerShift` | 布林带缓冲区的平移量（不支持负值）。 | `0` |
| Indicators | `BollingerDeviation` | 布林带标准差倍数。 | `2` |
| Indicators | `MovingAveragePeriod` | OsMA 平滑移动平均长度。 | `10` |
| Indicators | `MovingAverageShift` | 移动平均缓冲区的平移量（不支持负值）。 | `0` |
| Indicators | `MovingAverageMethod` | 移动平均类型（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 | `Simple` |

## 实现说明
- 使用 `WhenCandlesFinished` 只在完整的蜡烛线生成信号。
- 指标值保存在历史数组中以模拟 MetaTrader 的缓冲区平移；请使用 0 或正数的 shift 值。
- 追踪止损基于蜡烛收盘价进行更新，若需要逐笔行情级别的追踪请适当调整点差距离。

## 使用步骤
1. 在 StockSharp 中选择交易品种与时间框架。
2. 根据需要调整 `CandleType`、`LotSize` 与各类指标参数。
3. 启动策略后即可自动订阅蜡烛线、计算指标并按照上述逻辑执行交易。
