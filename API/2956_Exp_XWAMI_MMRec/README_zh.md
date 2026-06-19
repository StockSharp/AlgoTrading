# Exp XWAMI MMRec（ID 2956）

## 摘要

该策略移植自 MetaTrader 专家顾问 **Exp_XWAMI_MMRec**，结合了自定义的 XWAMI 动量指标与“损失计数”资金管理模块。动量被定义为当前价格与 `Period` 根之前价格的差值，该差值依次经过四级可配置平滑器处理；第 3、4 级平滑输出分别重现原始指标的 `Up` 与 `Down` 缓冲区，交叉点用于触发多空反转。

每级平滑器都支持多种算法：简单/指数/平滑/线性加权均线、Jurik JJMA/JurX、Tillson T3、VIDYA（用 EMA 近似）以及 Kaufman AMA。策略只维护单一净头寸，可做多亦可做空。资金管理模块会在最近 `BuyTotalTrigger` / `SellTotalTrigger` 笔交易中统计亏损次数，当亏损数达到 `BuyLossTrigger` / `SellLossTrigger` 阈值时，将下一笔交易的手数降到 `ReducedVolume`。

止损与止盈遵循原始 EA：`StopLossPoints` 与 `TakeProfitPoints` 以合约最小价位(`Security.PriceStep`)为单位。当在信号周期内触及止损/止盈时立即平仓，并将盈亏记入资金管理历史。

## 参数对照

| StockSharp 属性 | 默认值 | 原始输入 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | H1 时间框架 | `InpInd_Timeframe` | 指标所用的 K 线周期。 |
| `Period` | 1 | `iPeriod` | 计算动量时比较的历史 K 线位移。 |
| `Method1` / `Length1` / `Phase1` | `T3`, `4`, `15` | `XMethod1`, `XLength1`, `XPhase1` | 第一级平滑方式、周期与相位（相位仅对 Jurik/JurX/T3 有效）。 |
| `Method2` / `Length2` / `Phase2` | `Jjma`, `13`, `15` | `XMethod2`, `XLength2`, `XPhase2` | 第二级平滑设置。 |
| `Method3` / `Length3` / `Phase3` | `Jjma`, `13`, `15` | `XMethod3`, `XLength3`, `XPhase3` | 第三级平滑设置（原指标的 `Up` 缓冲区）。 |
| `Method4` / `Length4` / `Phase4` | `Jjma`, `4`, `15` | `XMethod4`, `XLength4`, `XPhase4` | 第四级平滑设置（原指标的 `Down` 缓冲区）。 |
| `AppliedPrice` | `Close` | `IPC` | 指标输入价格，支持全部 MetaTrader 价格选项（含 TrendFollow 与 Demark 价格）。 |
| `SignalBar` | 1 | `SignalBar` | 用于判定信号的历史 K 线索引（`0` 表示最近一根已完成 K 线）。 |
| `AllowBuyOpen` / `AllowSellOpen` | `true` | `BuyPosOpen`, `SellPosOpen` | 是否允许开多/开空。 |
| `AllowBuyClose` / `AllowSellClose` | `true` | `BuyPosClose`, `SellPosClose` | 是否允许在反向信号出现时强制平仓。 |
| `NormalVolume` | `0.1` | `MM` | 正常开仓手数。 |
| `ReducedVolume` | `0.01` | `SmallMM_` | 发生连续亏损后的降级手数。 |
| `BuyTotalTrigger` / `BuyLossTrigger` | `5` / `3` | `BuyTotalMMTriger`, `BuyLossMMTriger` | 多单资金管理窗口长度与亏损阈值。 |
| `SellTotalTrigger` / `SellLossTrigger` | `5` / `3` | `SellTotalMMTriger`, `SellLossMMTriger` | 空单资金管理窗口长度与亏损阈值。 |
| `StopLossPoints` | `1000` | `StopLoss_` | 止损距离（点）。 |
| `TakeProfitPoints` | `2000` | `TakeProfit_` | 止盈距离（点）。 |

## 运行逻辑

1. 订阅所需 K 线并仅处理已完成的柱体。
2. 计算当前 `AppliedPrice` 与 `Period` 根前价格的差值，并在历史足够时依次通过四级平滑器。
3. 保存第 3、4 级输出。若 `SignalBar + 1`（上一根 K 线）上 `Up` 与 `Down` 发生交叉，则更新交易方向：`Up > Down` 时先平空，再在 `SignalBar` 上满足 `Up <= Down` 时开多；反之亦然。
4. 根据最近成交结果决定手数：检查最近 `BuyTotalTrigger`（或 `SellTotalTrigger`）笔交易的盈亏，若其中亏损数达到阈值，则下一次使用 `ReducedVolume`，否则仍用 `NormalVolume`。
5. 若持有多单，则将止损/止盈点数换算为价格（乘以 `Security.PriceStep`），当日内最高价/最低价触碰该价格时立即平仓并记录盈亏；空单采用对称规则。

## 与原版 EA 的差异

- StockSharp 仅维护净头寸，不需要 `BuyMagic` / `SellMagic`、全局变量以及 `MarginMode` 相关逻辑。
- Tillson T3 在代码中直接实现；Jurik JJMA 与 JurX 均映射到 `JurikMovingAverage` 并保留相位。由于缺乏原生实现，VIDYA 与 ParMA 近似为指数均线。
- 通过 `BuyMarket` / `SellMarket` 下市价单，并在策略内部监控 K 线高低点触发止损/止盈，而非使用 MT5 的挂单。
- 原策略中的滑点 `Deviation_` 在 StockSharp 订单模型中不再需要。

## 使用提示

1. 选择交易品种并设置 `CandleType` 为原策略所使用的时间框架。
2. 调整各级平滑方法与长度以匹配 MetaTrader 设置。
3. 根据风险偏好配置 `NormalVolume`、`ReducedVolume` 及触发阈值。
4. 将策略附加到投资组合并启动，系统会在每次指标交叉时自动反转头寸。

如需扩展，可修改 `ExpXwamiMmRecStrategy.CreateFilter` 中的映射以接入其他 StockSharp 指标。
