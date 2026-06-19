# Bull Row Breakout 策略

## 概览
Bull Row Breakout 策略是 MetaTrader 5 专家顾问 “BULL row full EA” 的 C# 版本。原策略使用模块化构建，结合蜡烛排列和动量确认。移植到 StockSharp 后，我们在单一可配置的时间框架上复现相同逻辑，并按照仓库要求将代码注释保持为英文。

当一组看跌蜡烛被看涨动量和向上突破取代时，策略只做多头。Stochastic 随机指标用于过滤动量，而动态止损与止盈重建了 MQL 版本的风险设置。

## 入场流程
1. 仅在新蜡烛收盘时评估信号（“每根柱子一次”）。
2. 如果当前没有多头仓位，继续检测条件。
3. 看跌排列：
   - 从 `BearShift` 开始向前数 `BearRowSize` 根蜡烛必须全部收阴。
   - 每根蜡烛实体至少达到 `BearMinBody` 个价格步长。
   - 实体变化需符合 `BearRowMode`（普通 / 逐渐增大 / 逐渐减小）。
4. 看涨排列：
   - 从 `BullShift` 开始向前数 `BullRowSize` 根蜡烛必须全部收阳。
   - 每根蜡烛实体至少达到 `BullMinBody` 个价格步长。
   - 实体变化需符合 `BullRowMode`。
5. 突破确认：最近收盘价要高于第 2 根到第 `BreakoutLookback` 根历史蜡烛的最高价。
6. 随机指标确认：
   - 当前 %K（`StochasticKPeriod`）必须高于 %D（`StochasticDPeriod`）。
   - 过去 `StochasticRangePeriod` 个 %K 值全部位于 `StochasticLowerLevel` 与 `StochasticUpperLevel` 之间。
7. 风险控制：
   - 止损价格取自最近 `StopLossLookback` 根蜡烛的最低价。
   - 止盈价格等于止损距离的 `TakeProfitPercent`%。
   - 每根蜡烛收盘时检测是否触发止损或止盈，若触发则用 `SellMarket` 平仓。

## 参数说明
| 参数 | 说明 |
| --- | --- |
| `Volume` | 每次入场使用的固定交易量。 |
| `CandleTimeFrame` | 参与计算的蜡烛时间框架。 |
| `StopLossLookback` | 计算动态止损所使用的历史蜡烛数量。 |
| `TakeProfitPercent` | 止盈相对于止损距离的百分比。 |
| `BearRowSize`、`BearMinBody`、`BearRowMode`、`BearShift` | 看跌排列设置。 |
| `BullRowSize`、`BullMinBody`、`BullRowMode`、`BullShift` | 看涨排列设置。 |
| `BreakoutLookback` | 用于突破确认的最高价回溯长度。 |
| `StochasticKPeriod`、`StochasticDPeriod`、`StochasticSlowing` | 随机指标参数。 |
| `StochasticRangePeriod` | 需要保持在通道内的 %K 历史值数量。 |
| `StochasticUpperLevel`、`StochasticLowerLevel` | %K 通道上下界。 |

蜡烛实体长度以价格步长表示，对应原版中 `toDigits` 的处理方式。当品种没有提供价格步长时，默认使用 1。

## 与 MQL 版本的差异
- 原策略可以为每个模块选择不同的时间框架，此移植版在单一的 `CandleTimeFrame` 上运行，以匹配最常见的使用方式。
- 模块化代码中的虚拟止损和挂单管理未在移植版中实现。
- 止损与止盈通过检测蜡烛实现，一旦价格越过水平即调用 `SellMarket` 平仓。
- 原有的图形对象与状态显示未移植。

## 使用建议
- 根据交易品种优化蜡烛序列的长度与偏移；默认值复现了原策略的设定（向前 3 根看跌 + 向前 2 根看涨）。
- 可通过调整 `StochasticLowerLevel` 和 `StochasticUpperLevel` 改变过滤强度。
- 由于止损依赖最近低点，出现跳空的市场可能需要增大 `StopLossLookback` 或增加额外过滤条件。
