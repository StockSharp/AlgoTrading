# 十五分钟剥头皮策略
[English](README.md) | [Русский](README_ru.md)

本策略将 **15M Scalper** MetaTrader 智能交易系统移植到 StockSharp 高层 API。移植版本保留了原始 EA 的所有筛选器
（加权移动平均、随机指标、Parabolic SAR、更高周期的 Momentum 以及月度 MACD）以及复杂的风险控制模块，包括点数
止盈/止损、资金止盈、资金回撤追踪、保本机制以及权益回撤保护。逻辑严格基于完成的 K 线，与 MQL 实现保持一致。

## 工作流程

1. **趋势过滤**：在主周期（默认 15 分钟）上计算两条以典型价 (`(高+低+收)/3`) 为输入的加权移动平均线。做多时要求
   快线高于慢线，做空时要求快线低于慢线。
2. **随机指标反转**：读取 5/3/3 随机指标在前两根收盘 K 线上的数值。多头需要 %K 从 20 下方向上穿越，空头需要
   %K 从 80 上方向下穿越，与原脚本中的 `Stoc1`/`Stoc2` 判断完全一致。
3. **Parabolic SAR 确认**：最近收盘 K 线上的 SAR 值必须位于前一根 K 线开盘价的下方（多头）或上方（空头），复现
   `SAR < Open[1]` / `SAR > Open[1]` 的安全过滤。
4. **高周期 Momentum**：在可配置的高周期（默认 1 小时）上计算 14 周期 Momentum，并保存最近三根完成 K 线相对于
   100 水平的绝对偏离。只要任一偏离值超过设定阈值即可满足动量确认条件。
5. **月度 MACD**：在更大周期（默认 30 天）的 K 线上计算 MACD。多头要求主线高于信号线，空头要求主线低于信号线。
   同一个 MACD 序列也可用于提前离场（`UseExitByMacd`）。
6. **仓位处理**：当出现反向信号时，策略先平掉当前仓位，并在下一根 K 线重新评估入场。仓位规模遵循 EA 的
   “加码”规则：`LotExponent` 控制每次加仓的倍数，`IncreaseFactor` 在连续亏损后增大基准仓位。

## 风险控制

- **点数止损/止盈**：`StopLossSteps` 与 `TakeProfitSteps` 以点数输入，通过 `Security.PriceStep` 转换为绝对价格差。若
  价格步长小于 1，则自动乘以 10，以模拟 MetaTrader 的“pip”概念。
- **保本移动**：当价格向有利方向移动 `BreakEvenTriggerSteps` 点后，将虚拟止损移动到开仓价加 `BreakEvenOffsetSteps`
  偏移，一旦价格回落穿越该水平，立即市价离场。
- **跟踪止损**：记录自入场以来的最高价（多头）或最低价（空头）。若回撤幅度超过 `TrailingStopSteps`，按市价平仓，
  等价于 EA 中的 `OrderModify` 逻辑。
- **资金类目标**：`UseProfitTargetMoney`、`UseProfitTargetPercent` 与 `EnableMoneyTrailing` 使用 `PriceStep × StepPrice`
  计算浮动盈亏，从而实现固定金额止盈、按初始资金百分比止盈以及资金回撤追踪 (`MoneyTrailingStop`) 等功能。
- **权益回撤保护**：`UseEquityStop` 记录初始资金 + 已实现盈亏 + 浮动盈亏之和的历史最大值，一旦当前值回撤超过
  `TotalEquityRisk` 百分比，立即清空仓位。
- **马丁加仓**：同向加仓时按 `LotExponent` 的指数倍放大仓位；如果连续出现亏损，`IncreaseFactor` 会按亏损笔数对
  下一次基准仓位进行增量调整。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 主循环使用的 K 线类型（默认 15 分钟）。 |
| `MomentumCandleType` | 用于 Momentum 筛选的高周期 K 线（默认 1 小时）。 |
| `MacdCandleType` | 用于 MACD 筛选的 K 线（默认 30 天）。 |
| `FastMaPeriod`, `SlowMaPeriod` | 快/慢 LWMA 的周期，用于判断趋势方向。 |
| `MomentumPeriod` | 高周期 Momentum 的计算长度。 |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | 动量偏离 100 的最小阈值（多头/空头）。 |
| `StopLossSteps`, `TakeProfitSteps` | 以价格步长计的止损、止盈距离（0 表示禁用）。 |
| `TrailingStopSteps` | 跟踪止损的距离（价格步长单位）。 |
| `UseMoveToBreakeven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | 是否启用保本、触发距离与偏移量。 |
| `UseProfitTargetMoney`, `ProfitTargetMoney` | 是否启用固定金额止盈及其目标值。 |
| `UseProfitTargetPercent`, `ProfitTargetPercent` | 是否启用百分比止盈及目标百分比（基于初始资金）。 |
| `EnableMoneyTrailing`, `MoneyTrailingTakeProfit`, `MoneyTrailingStop` | 资金回撤追踪的触发值与允许回撤。 |
| `UseEquityStop`, `TotalEquityRisk` | 是否启用权益回撤保护以及允许的最大回撤百分比。 |
| `BaseVolume`, `LotExponent`, `IncreaseFactor`, `MaxTrades` | 基础手数、加码倍数、亏损加仓系数和最大加仓次数。 |
| `UseExitByMacd` | 当 MACD 反向交叉时是否立即离场。 |

## 使用步骤

1. 将策略附加到交易品种，确认 `Security.PriceStep` 与 `Security.StepPrice` 均已填写。这两个值用于将点数和资金参数
   转换为绝对数值。
2. 根据需要调整 `CandleType`、`MomentumCandleType`、`MacdCandleType`，以匹配目标市场的时间结构。默认值与原 EA 的
   M15/H1/Monthly 组合一致。
3. 根据品种波动幅度设置点数型参数（止损、止盈、跟踪止损、保本触发等）。
4. 配置资金管理选项：决定是否使用资金/百分比止盈、是否启用资金回撤跟踪，以及是否激活权益回撤保护。
5. 启动策略。系统会自动订阅所需的所有 K 线数据，并在指标准备就绪后开始评估交易机会；如果图表可用，也会绘制对应的
   指标和成交记录，方便对比原始 EA。

## 与原始 EA 的差异

- StockSharp 使用聚合仓位模型。出现反向信号时先平仓，再在下一根 K 线上评估新方向，避免同时持有多空订单。
- 资金类逻辑依赖 `Security.PriceStep` 与 `Security.StepPrice`。若交易所未提供这些数据，则浮动盈亏视为 0，对应的资金
  止盈/回撤功能会自动跳过。
- 由于测试环境无法获取真实账户保证金，`IncreaseFactor` 被实现为“每次亏损按系数线性增加基础手数”，仍然保留了原策略
  “连续亏损加仓”的意图。
- 所有判断均在 K 线收盘后执行，与 MQL 中使用 `iMA/iStochastic(..., shift)` 访问历史数据的方式一致，避免重复触发。
- 如果界面提供图表，策略会绘制同样的指标，便于与 MetaTrader 版本做视觉对比与调试。

在实盘部署前，请务必确认交易品种的最小价格变动、价格步长价值以及成交量步长等信息，因为这些参数直接决定了点数到价
格、资金目标的换算结果。
