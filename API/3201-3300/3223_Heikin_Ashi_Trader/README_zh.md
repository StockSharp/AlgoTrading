# Heikin Ashi Trader 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 4 专家顾问 “Heikin Ashi Trader” 移植到 StockSharp。原版 EA 的多指标确认流程被完整保留，并通过高级的蜡烛订阅 API 实现，因此所有决策都基于已完成的 K 线。

## 细节
- **指标组合**：
  - 在工作周期上计算的 Heikin-Ashi 蜡烛。
  - 使用典型价格的两条线性加权移动平均线（LWMA）。
  - 参数可调的随机指标 `%K/%D/Smooth`。
  - 以 100 为中枢的 Momentum 指标。
  - MACD。
- **入场条件**：
  - **做多**：最新 Heikin-Ashi 蜡烛为阳线；最近三次随机指标中至少一次高于超买线；快 LWMA 高于慢 LWMA；Momentum 与 100 的差值大于做多阈值；MACD 主线高于信号线。
  - **做空**：相反条件——Heikin-Ashi 阴线，随机指标低于超卖线，快线低于慢线，Momentum 差值大于做空阈值，MACD 主线低于信号线。
  - 可选地在入场前平掉反向仓位（`CloseOppositePositions`）。
- **仓位管理**：
  - 按点数设置的固定止损、止盈（基于标的的最小报价步长换算）。
  - `TrailingStopPips` 定义的可选追踪止损。
  - Break-even 模块：当利润达到 `BreakEvenTriggerPips` 时，将止损移动到 `入场价 ± BreakEvenOffsetPips`。
  - `ForceExit` 允许在下一根 K 线上强制平仓。
- **与 MT4 原版的差异**：
  - 原策略在更高周期计算 Momentum。为了保持高阶 API 的一致性，本移植在主周期上运行所有指标，并保留可调阈值以调整灵敏度。
  - MT4 代码中的资金止盈/止损功能未迁移，风险控制由价格止损与 Break-even 负责。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 用于所有计算和交易决策的蜡烛类型或时间框架。 |
| `FastMaPeriod`, `SlowMaPeriod` | 典型价格 LWMA 的快、慢周期。 |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | 随机指标的 `%K/%D` 以及平滑参数。 |
| `StochasticOverbought`, `StochasticOversold` | 最近三次随机指标需要达到的超买/超卖阈值。 |
| `MomentumPeriod` | Momentum 指标长度。 |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | 多/空方向 Momentum 偏离 100 的最小值。 |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD 设置。 |
| `CloseOppositePositions` | 入场前是否平掉反向仓位。 |
| `MaxPositions` | 每个方向的最大净仓（`0` 表示不限）。 |
| `TradeVolume` | 每次下单的手数，同时会写入策略的 `Volume`。 |
| `UseStopLoss`, `StopLossPips` | 是否启用止损及其点数。 |
| `UseTakeProfit`, `TakeProfitPips` | 是否启用止盈及其点数。 |
| `UseTrailingStop`, `TrailingStopPips` | 追踪止损开关及距离。 |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Break-even 启动阈值与偏移。 |
| `ForceExit` | 设为 `true` 时下一根 K 线直接平仓。 |

## 实现说明
- 通过 `SubscribeCandles().BindEx(...)` 绑定指标，避免直接访问 `GetValue()`，并确保使用完成的蜡烛数据。
- 点值转换基于 `PriceStep`。若标的存在“半点”报价，请在证券设置中配置正确的步长。
- 追踪止损与 Break-even 仅向有利方向移动。每次平仓都会重置缓存的止损/止盈，保证新仓位使用最新的风险参数。
