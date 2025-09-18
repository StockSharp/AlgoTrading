# 市场大师策略

## 概述

`MarketMasterStrategy` 是 MetaTrader 4 智能交易系统 “Market Master”（`MQL/31326/MarketMaster EN.mq4`）的 StockSharp 高级移植版本。原始 EA 将丰富的指标组合、复杂的资金管理、新闻过滤以及分批加仓结构整合在一起。移植时我们保留了确定性的技术核心，使其能够在 StockSharp 的事件驱动框架中无需外部网络服务即可运行。策略仅订阅一个蜡烛时间框架，并按照仓库规范使用 `Bind` 将所有指标连接到同一数据流。

## 指标体系

- **AverageTrueRange（ATR）**：维护两个实例。主 ATR 用于首笔入场，副 ATR 模拟 MT4 版本用于“对冲/恢复单”的过滤。
- **MoneyFlowIndex（MFI）**：衡量量价配合，帮助识别资金净流入或流出。
- **BullsPower / BearsPower**：复制 MT4 中的 `iBullsPower` 与 `iBearsPower` 指标，在开仓前确认多头或空头的力量占优。
- **StochasticOscillator**：提供 `%K` 与 `%D` 两条线，长度与减缓参数完全遵循原始输入，可根据需要启用或禁用过滤。
- **ParabolicSar**：保留两个独立的 SAR 指标（主确认与辅助确认），对应 MT4 中的不同时间框架设置。

所有指标都通过 StockSharp 自动预热。代码没有调用 `GetValue()`，而是使用 `_prevAtr`、`_prevMfi`、`_prevStochasticMain` 等字段保存上一根蜡烛的数值，符合仓库对高阶 API 的要求。

## 信号逻辑

原专家顾问包含 “ZERO” 与 “MA” 两套条件，它们共享 ATR/MFI/Bulls/Bears 的过滤器，而振荡器确认不同。C# 版本实现了更严格的 “MA” 分支，因为它更贴近真实盘面。多头信号需要以下条件在蜡烛收盘后同时成立：

1. ATR 相比上一根蜡烛上升；当已经持仓时改用对冲 ATR 判断。
2. MFI 上升且 Bears Power 为正，说明多头动能增强。
3. 启用随机指标时，`%K` 高于 `%D` 且正在向上拐头，同时 `%K` 低于 `StochasticBuyLevel` 所设的超买上限。
4. 启用 SAR 过滤时，收盘价高于两个 SAR 值。
5. 蜡烛成交量大于 `MinVolume`（首单）或 `MinHedgeVolume`（加仓单）。

空头信号条件完全镜像：ATR 上升、MFI 下降、Bulls Power 为负、`%K` 低于 `%D`、价格落在 SAR 之下并满足成交量阈值。

## 仓位与风控

- **自动仓位**：`CalculateBaseVolume` 参照原 EA 的资金管理，通过 `RiskMultiplier` 将投资组合余额换算成下单手数，同时遵循品种的 `VolumeStep`、`MinVolume` 和 `MaxVolume` 限制。
- **分批加仓**：若 `AllowSameSignalEntries` 为真，则在已有仓位的同向信号出现时按照 `VolumeMultiplier` 放大下单量。StockSharp 采用净头寸模型，因此加仓实际上扩大净敞口，而不是维护多张独立订单。
- **反向信号**：`AllowOppositeEntries` 控制是否在识别到反向条件时立即平掉当前仓位并（可选）反手开仓。若关闭该选项，策略只会平仓，等待新的同向信号。
- **止损**：`StopLossPoints` 对应 MT4 的 `StopLoss`，若品种提供 `PriceStep`，将通过 `StartProtection` 转换为内置保护单。
- **交易时段**：`UseTradingWindow`、`TradingStart`、`TradingEnd`、`UseTradingBreak`、`BreakStart`、`BreakEnd` 重现 MT4 中的开盘区间与暂停窗口。比较时刻使用蜡烛消息自带的交易所时间。

## 与 MT4 版本的差异

- **新闻过滤**：原代码依赖 Investing.com 与 DailyFX 的网络日历。移植版不进行任何 HTTP 调用，可通过调整交易时间段或外部暂停策略来规避高影响事件。
- **订单历史判断**：诸如 `OrdersHistoryTotal()`、`OpenNewBuy()` 等函数基于 MT4 的票据系统。StockSharp 使用净仓模式，因此移植版在方向过滤重新满足时允许再次开仓，无需检查历史订单数量。
- **恢复单**：原 EA 会为每张订单设置不同的 Magic Number 和备注。C# 版保留手数倍增逻辑，但所有加仓都体现在同一净头寸上。
- **跟踪止损**：MT4 的 `TrailingStop`/`TrailingStep` 通过修改挂单实现。移植版未包含该部分，用户可以在 `StartProtection` 中添加追踪参数或自行订阅 `PositionChanged` 事件实现高级管理。

## 参数对照

| 属性 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | `1` | 禁用自动仓位时的基础手数。 |
| `UseAutoVolume` | `true` | 是否启用资金规模换算。 |
| `RiskMultiplier` | `10` | 自动仓位使用的风险乘数，对应 `Risk_Multiplier`。 |
| `VolumeMultiplier` | `2` | 加仓时的倍数，对应 `KLot`。 |
| `MinVolume` | `3000` | 首次入场所需的最小成交量 (`MinVol`)。 |
| `MinHedgeVolume` | `3000` | 加仓/反手所需的成交量 (`MinVolH`)。 |
| `AtrPeriod` / `AtrHedgePeriod` | `14` | 主 ATR 与对冲 ATR 的周期。 |
| `MfiPeriod` | `14` | MFI 周期。 |
| `BullBearPeriod` | `14` | Bulls/Bears Power 周期。 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | `5 / 3 / 3` | 随机指标参数。 |
| `StochasticBuyLevel` / `StochasticSellLevel` | `60 / 40` | 随机指标买入/卖出阈值 (`StoBuy` / `StoSell`)。 |
| `UseStochasticFilter` / `UsePsarFilter` / `UsePsarConfirmation` | `true` | 是否启用对应过滤器。 |
| `PsarStep` / `PsarMaxStep` / `PsarConfirmStep` / `PsarConfirmMaxStep` | `0.02 / 0.2 / 0.02 / 0.2` | 两组 SAR 的步长与上限。 |
| `AllowSameSignalEntries` | `false` | 允许同向加仓。 |
| `AllowOppositeEntries` | `true` | 允许立即反手。 |
| `UseTradingWindow` | `false` | 启用交易时间过滤。 |
| `TradingStart` / `TradingEnd` | `06:00 / 18:00` | 每日交易窗口。 |
| `UseTradingBreak` | `false` | 启用中途暂停。 |
| `BreakStart` / `BreakEnd` | `06:00:01 / 06:00:02` | 暂停时间段。 |
| `StopLossPoints` | `0` | 以点数表示的止损距离。 |
| `CandleType` | `15m TimeFrame` | 信号所用蜡烛类型。 |

## 使用建议

1. 在 StockSharp Designer 或代码中连接证券与投资组合，并提前启动策略让指标充分预热。
2. 如需多时间框架确认，可修改 `CandleType` 或两个 SAR 的参数。所有指标均通过 `Bind` 绑定，无需手动注册。
3. 若扩展策略逻辑，建议使用 `LogInfo`/`LogWarning` 记录调试信息。当前实现保持状态管理简单，方便叠加追踪止损、风险开关等模块。
4. 策略基于净头寸。若希望模拟 MT4 的多票据结构，可在外层封装一个自定义订单路由器来拆分净仓。

## 扩展方向

- 重写 `OnNewMyTrade` 或监听 `PositionChanged` 以添加自定义离场规则。
- 构建外部新闻模块，根据经济数据窗口自动修改 `UseTradingWindow` 或直接调用 `Stop()`。
- 需要可视化时，可在 `OnStarted` 中创建图表区域并绘制指标；移植版为保持清晰未附带默认图表。

本代码遵循仓库规范：使用制表符缩进、通过高层 `Bind` 订阅指标、避免访问指标历史、并用 `StrategyParam` 暴露全部可调输入。
