# Engulfing Momentum 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 上的 **ENGULFING** 顾问移植到 StockSharp 的高级 API。它在工作周期上寻找多头/空头吞没形态，并结合高周期动量确认以及近似月线（30 天）的 MACD 趋势过滤。止损、止盈、移动止损与保本逻辑全部使用价格步长实现，以匹配原始 EA 的行为。

## 运行逻辑

1. **吞没形态**：最新完结的 K 线必须吞没前一根 K 线的实体。策略还会检查前两根 K 线之间存在重叠，以模拟原始代码中的分形条件。
2. **趋势过滤**：使用快速与慢速加权移动平均线（LWMA 对应物）。做多需要快速均线在慢速均线上方，做空则相反。
3. **动量过滤**：在较高时间框（默认 1 小时）上计算 14 周期 Momentum，只要最近三次输出中任意一次相对 100 的偏离大于阈值即可视为有效（对应 MQL 中的 `MomLevelB/MomLevelS`）。
4. **MACD 过滤**：在 `MacdCandleType`（默认 30 天）上，MACD 主线必须位于信号线之上才能做多，反之才能做空，与原 EA 中的 `MacdMAIN0` 与 `MacdSIGNAL0` 判定一致。
5. **仓位管理**：出现反向信号时直接反手。只要达到止损、止盈、保本或移动止损条件，就立即退出当前仓位。

## 风险控制

- **止损 / 止盈**：以价格步长（tick）为单位设置，对应原始输入 `Stop_Loss` 与 `Take_Profit`。设为 0 可关闭。
- **移动止损**：可选，按照设置的步长追踪最高/最低价格。
- **保本**：当浮动盈利达到 `BreakEvenTriggerSteps` 时，将止损移动到入场价加上 `BreakEvenOffsetSteps` 的距离，实现 EA 中的“no loss”功能。

原脚本中的金额或百分比止盈 (`Use_TP_In_Money`, `Take_Profit_In_percent`) 未实现；在 StockSharp 中更推荐通过步长参数来调节同等效果。

## 参数说明

- `FastMaPeriod`, `SlowMaPeriod`：快速/慢速加权移动平均的周期。
- `MomentumPeriod`：高周期 Momentum 的周期数。
- `MomentumBuyThreshold`, `MomentumSellThreshold`：动量偏离 100 的最小阈值。
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength`：MACD 的三项参数。
- `StopLossSteps`, `TakeProfitSteps`：止损、止盈距离（价格步长）。
- `TrailingStopSteps`：移动止损距离，0 表示禁用。
- `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps`：触发保本所需的步长以及保本后的偏移。
- `CandleType`：主要分析的时间框。
- `HigherCandleType`：动量过滤使用的高时间框（默认 1 小时）。
- `MacdCandleType`：MACD 过滤使用的时间框（默认 30 天 ≈ 1 个月）。

## 使用步骤

1. 选择目标证券并根据需求设置 `CandleType`、`HigherCandleType` 与 `MacdCandleType`。
2. 根据品种波动性调整均线周期与动量阈值。
3. 按照合约最小价位变动设置止损、止盈、移动止损及保本距离。
4. 启动策略，指标形成后即会自动评估信号并控制仓位。

## 与原 EA 的差异

- 使用 `WeightedMovingAverage` 等内置指标，无需手写 LWMA 计算。
- 保本和移动止损基于收盘 K 线执行，符合原策略的逐 bar 行为，同时利用 StockSharp 的保护机制。
- 金额/百分比目标未移植，可通过调节步长参数实现类似效果。
- 策略仅持有单一方向仓位，尽管 EA 提供 `Max_Trades` 参数，但常见用法同样是单仓操作。

在波动较大的市场上，可适当增大动量阈值以及止损、止盈步长，以避免过早出场或噪声信号。
