# 日元交易者 05.1 策略（C#）

## 概述

日元交易者 05.1 策略重现了原始 MetaTrader 智能交易系统，通过三条货币对之间的三角关系寻找突破机会：

- **交易交叉盘** —— 本策略实例绑定的证券（例如 GBPJPY）。
- **主要货币对** —— 交叉盘基准货币与美元的组合（例如 GBPUSD），用于捕捉主驱动趋势。
- **USDJPY** —— 用于确认日元腿的动量。

当主要货币对出现突破并得到 USDJPY 的同步确认时，策略会在交叉盘上建立头寸。可选的 RSI、CCI、RVI 与均线过滤器帮助筛选信号。仓位管理同时支持金字塔加仓与摊平补仓，风险控制部分完整复刻了 EA 中基于点差和 ATR 的止损逻辑。

## 交易逻辑

1. **突破检测**
   - `LoopBackBars` 指定回溯窗口。当大于 1 时，策略会检查：
     - 最近的最高价/最低价（`PriceReference = HighLow`），或
     - `LoopBackBars` 根之前的收盘价（`PriceReference = Close`）。
   - `MajorDirection` 决定主要货币对与日元腿应当如何协同（Left 表示交叉盘报价为主要货币/日元，Right 表示日元/主要货币）。
2. **过滤器**
   - `UseRsiFilter` 要求 RSI 位于 50 上方或下方以确认趋势方向。
   - `UseCciFilter` 强制 CCI 为正或为负。
   - `UseRviFilter` 等待 RVI 上穿或下穿其信号线。信号线使用 4 周期简单移动平均，与 MT4 中的 `MODE_SIGNAL` 一致。
   - `UseMovingAverageFilter` 通过指定周期与类型的均线确保入场方向顺应趋势。
3. **入场模式**
   - `EntryMode = Both` 允许任意突破信号。
   - `EntryMode = Pyramiding` 仅在顺势 K 线后加仓。
   - `EntryMode = Averaging` 仅在逆势 K 线后补仓摊平。
4. **仓位管理**
   - `FixedLotSize` 设定固定下单量。
   - 当固定手数为 0 时，使用 `BalancePercentLotSize` 与组合市值计算动态手数。
   - `MaxOpenPositions` 限制最大加仓次数（累积仓位规模）。
5. **风险控制**
   - 所有点差参数（`StopLossPips`、`TakeProfitPips`、`BreakEvenPips`、`ProfitLockPips`、`TrailingStopPips`、`TrailingStepPips`）都会通过 `Security.MinPriceStep` 转换为价格距离。
   - 启用 `EnableAtrLevels` 后，策略改用 ATR 距离，ATR 通过 `AtrCandleType` 与 `AtrPeriod` 订阅的 K 线计算，倍数由各个乘数参数控制。
   - 止损、止盈、保本、锁盈与跟踪止损均基于收盘数据更新，保持与原 EA 相同的节奏。
   - `CloseOnOpposite` 在出现反向信号时会平掉已有仓位并按需要反向建仓。
   - `AllowHedging` 允许在持有反向仓位时继续加仓。由于 StockSharp 采用净仓位模型，无法真正同时持有多空仓，该选项仅决定策略是否允许在当前净仓与信号方向相反时直接翻仓。

## 参数一览

| 组别 | 名称 | 说明 |
|------|------|------|
| 仪表 | `MajorSecurity` | 用于确认的主要货币对。|
| | `UsdJpySecurity` | 用于确认日元腿的 USDJPY。|
| 数据 | `CandleType` | 三个货币对共享的信号级别。|
| 过滤 | `MajorDirection` | 主要货币对与交叉盘的方向关系。|
| | `PriceReference` | 突破参考类型：高低点或延迟收盘价。|
| | `LoopBackBars` | 回溯计算的历史根数。|
| | `EntryMode` | 加仓模式：同时支持、仅金字塔或仅摊平。|
| 指标 | `UseRsiFilter`、`UseCciFilter`、`UseRviFilter`、`UseMovingAverageFilter` | 是否启用各类过滤器。|
| | `MaPeriod`、`MaMode` | 均线周期与类型。|
| 风险 | `FixedLotSize`、`BalancePercentLotSize` | 下单量控制。|
| | `MaxOpenPositions` | 最大加仓次数。|
| | `StopLossPips` 等 | 基于点差的风险参数。|
| | `EnableAtrLevels` 及 ATR 相关参数 | 基于 ATR 的风险设置。|
| 行为 | `CloseOnOpposite` | 是否在反向信号时平仓。|
| | `AllowHedging` | 是否允许在反向净仓位情况下继续入场。|

## 使用建议

- 将交易交叉盘赋给策略的 `Security` 属性，同时配置 `MajorSecurity` 与 `UsdJpySecurity`。
- 动态手数需要组合估值，运行前确保投资组合已连接并更新 `Portfolio.CurrentValue`。
- 请保证三个货币对的 K 线时间轴一致，如来自不同市场，可预先统一到同一时间框架。
- ATR 订阅使用 `AtrCandleType` 指定的级别，保持默认（日线、21 周期）即可与原 EA 行为一致。
- 风险控制基于收盘价执行，触发后通过市价单平仓，模拟 EA 中的止损移动逻辑。

## 与 MT4 版本的差异

- StockSharp 以净仓位计算，不支持真正的对冲持仓。`AllowHedging` 仅决定策略是否允许自动翻仓。
- 原 EA 在 tick 级别修改挂单止损，本策略在收盘时检测阈值后以市价单离场。
- RVI 信号线通过对 RVI 值进行四周期 SMA 计算，与 MT4 的实现保持一致。

