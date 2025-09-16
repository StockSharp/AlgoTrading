# Exp Skyscraper Fix Color AML MMRec 策略

Exp Skyscraper Fix Color AML MMRec 是 MQL5 专家顾问 *Exp_Skyscraper_Fix_ColorAML_MMRec* 的 StockSharp 版本。原始机器人将 **Skyscraper Fix** 与 **Color AML** 两个独立指标结合，并通过 MMRec 资金管理逻辑在连续亏损后自动降低下单手数。C# 实现完整保留了双信号源和自适应仓位管理，同时使用 StockSharp 的高级 API 完成委托执行。

## 交易流程

1. **Skyscraper Fix 模块** 基于 `SkyscraperCandleType` 的完结K线构建自适应通道。当通道颜色转为青色（趋势 &gt; 0）时，所有空头仓位都会被平掉，并且如果上一根延迟样本不是青色，则会开出新的多单；当颜色转为红色（趋势 &lt; 0）时，逻辑对称用于做空。策略直接复用了 `3040_Exp_Skyscraper_Fix_Duplex` 中的 `SkyscraperFixIndicator`。
2. **Color AML 模块** 处理 `ColorAmlCandleType` 对应的K线。移植后的 `ColorAmlIndicator` 重现了自适应市场水平，并输出颜色代码：`2` 表示看多，`0` 表示看空，`1` 为中性。当检测到看多或看空颜色时，模块先行平掉反向仓位，若颜色与上一延迟样本发生变化，再开出新的顺势仓位。
3. **信号延迟** 由 `SkyscraperSignalBar` 与 `ColorAmlSignalBar` 各自控制。策略为两个指标维护独立的队列，只在累计到指定数量的完结K线后才执行交易，等价于原顾问中 `CopyBuffer(..., shift, ...)` 的行为。
4. **风控处理** 沿用了原始止损/止盈距离。两个模块分别在“价格步长”（tick）意义上定义防护距离，策略会换算成绝对价格并在每根完结K线上检查是否触碰到止损或止盈。一旦触发，立即通过市价单平仓，并清除所有保护阈值。
5. **MMRec 资金管理** 分别跟踪 Skyscraper 多头、Skyscraper 空头、Color AML 多头与 Color AML 空头的连续亏损次数。当某个方向的亏损连击达到对应的 `*LossTrigger` 时，下单量从 `*Mm` 切换为 `*SmallMm`；一旦出现盈利交易，亏损计数归零。由于示例策略在净头寸模式下运行，C# 版本仅对 `Lot` 模式提供实质效果，其余模式会退化为直接按手数下单。

## 实现说明

- 策略完全基于 StockSharp 高级 API：K线订阅驱动指标计算，交易通过 `BuyMarket`、`SellMarket` 与 `ClosePosition` 方法完成。
- 防护订单使用市价平仓实现，而非额外挂出止损/止盈指令，以避免两个模块共享净头寸时出现冲突。
- 资金管理在 `OnOwnTradeReceived` 中读取成交信息判定上一笔交易盈亏，并记录触发该仓位的模块，从而在平仓时精确更新对应的亏损计数。
- 移植的 `ColorAmlIndicator` 缓存所需的历史K线和光滑值，复刻了原脚本的动态 alpha 指数平滑算法以及颜色判定（蓝色代表 AML 上升，红色代表下降，灰色为中性）。
- 原 MQL5 版本中的魔术号与滑点参数在 StockSharp 环境下不再需要，因此未予实现。

## 参数

### Skyscraper Fix 模块

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `SkyscraperCandleType` | H4 K线 | 构建 Skyscraper Fix 通道所用的时间框架。 |
| `SkyscraperLength` | 10 | 计算自适应步长所使用的 ATR 回看窗口。 |
| `SkyscraperKv` | 0.9 | 作用于 ATR 步长的灵敏度乘数。 |
| `SkyscraperPercentage` | 0 | 施加到中轨上的百分比偏移。 |
| `SkyscraperMode` | HighLow | 通道所使用的价格（高/低或收盘）。 |
| `SkyscraperSignalBar` | 1 | 延迟 Skyscraper 信号的完结K线数量。 |
| `SkyscraperEnableLongEntry` | true | 通道转多时是否允许开多。 |
| `SkyscraperEnableShortEntry` | true | 通道转空时是否允许开空。 |
| `SkyscraperEnableLongExit` | true | 通道转空时是否平掉多单。 |
| `SkyscraperEnableShortExit` | true | 通道转多时是否平掉空单。 |
| `SkyscraperBuyLossTrigger` | 2 | 多头连续亏损达到该值后启用减仓手数。 |
| `SkyscraperSellLossTrigger` | 2 | 空头连续亏损达到该值后启用减仓手数。 |
| `SkyscraperSmallMm` | 0.01 | 启用减仓时使用的下单量。 |
| `SkyscraperMm` | 0.1 | 正常情况下 Skyscraper 模块的下单量。 |
| `SkyscraperMmMode` | Lot | 资金管理模式（C# 版仅 `Lot` 生效）。 |
| `SkyscraperStopLossTicks` | 1000 | 止损距离（tick）。为 0 表示禁用。 |
| `SkyscraperTakeProfitTicks` | 2000 | 止盈距离（tick）。为 0 表示禁用。 |

### Color AML 模块

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `ColorAmlCandleType` | H4 K线 | 计算 Color AML 所用的时间框架。 |
| `ColorAmlFractal` | 6 | AML 范围计算的分形窗口。 |
| `ColorAmlLag` | 7 | AML 指数平滑的滞后深度。 |
| `ColorAmlSignalBar` | 1 | 延迟 Color AML 信号的完结K线数量。 |
| `ColorAmlEnableLongEntry` | true | AML 变为看多（颜色 2）时是否开多。 |
| `ColorAmlEnableShortEntry` | true | AML 变为看空（颜色 0）时是否开空。 |
| `ColorAmlEnableLongExit` | true | AML 转空时是否平掉多单。 |
| `ColorAmlEnableShortExit` | true | AML 转多时是否平掉空单。 |
| `ColorAmlBuyLossTrigger` | 2 | 多头连续亏损达到该值后启用减仓手数。 |
| `ColorAmlSellLossTrigger` | 2 | 空头连续亏损达到该值后启用减仓手数。 |
| `ColorAmlSmallMm` | 0.01 | 启用减仓时使用的下单量。 |
| `ColorAmlMm` | 0.1 | 正常情况下 Color AML 模块的下单量。 |
| `ColorAmlMmMode` | Lot | 资金管理模式（C# 版仅 `Lot` 生效）。 |
| `ColorAmlStopLossTicks` | 1000 | 止损距离（tick）。为 0 表示禁用。 |
| `ColorAmlTakeProfitTicks` | 2000 | 止盈距离（tick）。为 0 表示禁用。 |

## 使用步骤

1. 将策略连接到目标投资组合与标的证券。标的必须提供 `SkyscraperCandleType` 与 `ColorAmlCandleType` 对应的K线序列。
2. 根据券商的合约手数调整资金管理参数。由于移植版本仅支持直接按手数下单，请合理配置 `*Mm` 与 `*SmallMm`。
3. 如有需要，可按 tick 调整两个模块各自的止损与止盈距离，将参数设为 0 即可禁用对应保护。
4. 启动策略后，程序会订阅两套K线，计算指标并按照上述规则自动管理持仓。

本文档描述的行为与 `CS/ExpSkyscraperFixColorAmlMmrecStrategy.cs` 完全一致，可作为在 StockSharp 中使用该策略的参考说明。
