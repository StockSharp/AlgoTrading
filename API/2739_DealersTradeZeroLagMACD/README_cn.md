# Dealers Trade ZeroLag MACD 策略

## 概述
该策略将 MetaTrader 平台上的 "Dealers Trade v 7.91 ZeroLag MACD" 专家顾问迁移到 StockSharp 高级 API。策略通过观察 ZeroLag MACD 线的斜率来判断市场方向，并在趋势方向上构建具有自适应间距的网格仓位，同时执行严格的风险控制。默认的分析周期为四小时 K 线，但可自定义任何支持的蜡烛类型。

## 运行逻辑
- **信号判定**：使用两条零延迟指数移动平均（ZLEMA）生成 MACD 线。当 MACD 相比前一根 K 线向上时视为看多，向下则视为看空；`ReverseCondition` 可以反向解释信号。
- **网格加仓**：在检测到的方向上逐步加仓。相邻建仓点之间的距离以点数(`IntervalPips`)表示，并按 `IntervalCoefficient` 逐步放大。每次加仓的手数按 `LotMultiplier` 乘法放大，复制原策略的马丁加仓方式。
- **仓位规模**：`BaseVolume` 大于零时直接作为初始手数；当其为零时，根据 `RiskPercent`、止损距离以及品种的价格步长自动推算手数，并确保不超过交易所限制及 `MaxVolume`。
- **仓位管理**：每笔仓位都可以设置止损、止盈以及跟踪止损（全部以点数表示）。后续仓位的止盈距离会按 `TakeProfitCoefficient` 扩大，以便更好地管理分层盈利。
- **账户保护**：当持仓数量超过 `PositionsForProtection` 且累计浮动利润达到 `SecureProfit` 时，策略会平掉盈利最高的仓位以锁定收益；若仓位数超过 `MaxPositions`，则会先行关闭浮动亏损最大的仓位。

## 仓位跟踪
- 策略只在蜡烛收盘后处理信号与风控逻辑，并使用收盘价、最高价、最低价进行判断。
- 每笔仓位都记录自己的成交价格、数量以及当前跟踪止损位置，最新成交价用于控制下一笔加仓的最小距离。
- 当账户余额低于 `MinimumBalance` 时策略自动停止，避免在资金不足时继续运行。

## 参数说明
- `BaseVolume`：初始下单数量（为 0 时启用按风险计算）。
- `RiskPercent`：按风险计算手数时使用的账户风险百分比。
- `MaxPositions`：允许同时持有的最大仓位数量。
- `IntervalPips` / `IntervalCoefficient`：网格初始间距及其放大系数。
- `StopLossPips`、`TakeProfitPips`：止损和止盈距离（点）。
- `TrailingStopPips`、`TrailingStepPips`：跟踪止损的距离与触发阈值。
- `TakeProfitCoefficient`：后续仓位止盈距离的放大倍数。
- `SecureProfit`、`AccountProtection`、`PositionsForProtection`：账户保护相关参数。
- `ReverseCondition`：是否反转 MACD 斜率的判断方向。
- `FastLength`、`SlowLength`、`SignalLength`：零延迟指数移动平均的周期。
- `MaxVolume`：单笔仓位的最大数量限制。
- `LotMultiplier`：每次加仓数量的放大倍数。
- `MinimumBalance`：继续交易所需的最低账户余额。
- `CandleType`：使用的蜡烛类型。

## 使用建议
1. 启动前需先绑定证券和投资组合，并确认交易所参数正确。
2. 检查品种的价格步长与步长价值，确保点数与实际价格变动之间的换算准确。
3. 默认参数与原 MQL 策略一致，可通过 StockSharp 的优化工具进一步优化。
4. 本任务仅提供 C# 版本，未包含 Python 实现。
