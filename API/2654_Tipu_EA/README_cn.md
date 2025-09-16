# Tipu EA 多周期策略

## 概述
该策略在 StockSharp 中重现 Tipu Expert Advisor 的主要思想。由于原始的 Tipu Trend 与 Tipu Stops 指标为专有实现，这里以指数移动平均线（EMA）、平均趋向指数（ADX）和平均真实波幅（ATR）的组合来完成同样的信号判断与风控。策略在较高周期（默认 1 小时）和信号周期（默认 15 分钟）之间寻找趋势共振，并通过保本加仓、移动止损及可选的固定止盈来管理持仓。

策略适用于流动性充足且具备趋势特征的市场。较高周期负责给出趋势背景并过滤震荡行情，而信号周期负责触发具体的入场条件。

## 订阅数据
- 较高周期 K 线（默认 1 小时），用于 EMA 趋势判断和 ADX 震荡过滤。
- 信号周期 K 线（默认 15 分钟），用于入场信号、ATR 止损计算以及交易管理。

## 交易逻辑
1. **高周期背景**
   - 计算快、慢 EMA 并检测金叉/死叉。金叉产生多头方向信号，死叉产生空头方向信号。
   - 使用 ADX 衡量趋势强度。当 ADX 低于阈值时认为市场处于震荡，不允许开新仓。
   - 记录最近一次高周期信号的时间戳，信号只在设定的时间窗口内有效。
2. **信号周期入场**
   - 必须同时满足信号周期上的 EMA 交叉和高周期最新信号方向一致，且高周期不在震荡状态。
   - 多头需要快 EMA 上穿慢 EMA，空头则反向。
   - 在下单前根据参数决定是否先平掉相反方向的持仓（反转平仓），并遵守是否允许对冲的设置。
   - 初始止损距离为 `ATR * AtrMultiplier`，同时受 `MaxRiskPips` 限制，若风险超出阈值则放弃该次入场。
3. **风险管理**
   - **固定止盈**：可选，用 `TakeProfitPips` 指定距离。
   - **移动止损**：当浮盈达到 `TrailingStartPips` 时，止损按 `TrailingCushionPips` 的缓冲距离跟随价格移动。
   - **保本加仓**：启用后，当浮盈达到 `RiskFreeStepPips` 时把止损移至盈亏平衡，同时按 `PyramidIncrementVolume` 的步长逐步加仓，直到达到 `PyramidMaxVolume`，每次加仓后都会收紧保护性止损。
   - 若 `CloseOnReverseSignal` 为真，则在出现反向信号时立即平掉当前仓位。

## 参数说明
- `AllowHedging` – 是否允许在持有反向仓位时继续加仓。
- `CloseOnReverseSignal` – 出现反向信号时是否直接平仓。
- `EnableTakeProfit`、`TakeProfitPips` – 是否启用固定止盈以及对应的点数。
- `MaxRiskPips` – 初始止损的最大允许点数，用于过滤风险过大的交易。
- `TradeVolume` – 首次入场的基础手数。
- `EnableRiskFreePyramiding`、`RiskFreeStepPips`、`PyramidIncrementVolume`、`PyramidMaxVolume` – 控制保本加仓模块。
- `EnableTrailingStop`、`TrailingStartPips`、`TrailingCushionPips` – 控制移动止损逻辑。
- `HigherFastLength`、`HigherSlowLength`、`LowerFastLength`、`LowerSlowLength` – 两个周期上使用的 EMA 长度。
- `AdxLength`、`AdxThreshold` – 高周期震荡过滤所用的 ADX 参数。
- `AtrLength`、`AtrMultiplier` – 初始止损所用 ATR 的周期和倍数。
- `HigherSignalWindowMinutes` – 高周期信号的有效时间（分钟）。
- `HigherCandleType`、`LowerCandleType` – 策略使用的两个时间周期。

## 实现细节
- 每次加仓后都会重新计算加权平均持仓价，保证移动止损与保本加仓模块基于真实成本进行判断。
- 所有决策均基于已完成的 K 线，未收盘的蜡烛不会触发信号，以避免噪声。
- 策略仅使用市价单（`BuyMarket`/`SellMarket`）完成开平仓，内部自行完成止损、止盈与加仓管理，无需挂出额外的条件单。
- 由于原始 Tipu 指标不可用，采用 EMA + ADX + ATR 的组合来逼近原始行为，同时完整保留反向平仓、保本加仓和移动止损等管理特色。

## 使用建议
- 在目标品种上优化 EMA 长度、ATR 倍数与 ADX 阈值，默认参数适合外汇主流货币对的初步测试。
- 将 `HigherSignalWindowMinutes` 设置为接近高周期长度可实现几乎同步的信号，也可适当放宽以允许一定时间差。
- 即使关闭加仓功能，保本模块仍会在达到 `RiskFreeStepPips` 后把止损移至盈亏平衡，从而提供基本的风险保护。
- 如果希望完全依靠移动止损退出，可关闭 `CloseOnReverseSignal` 让仓位保持到止损触发。
