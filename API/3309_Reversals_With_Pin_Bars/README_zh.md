# 针形反转策略

## 概述
本策略是 MetaTrader 专家顾问 **“Reversals With Pin Bars”** 的 C# 版本。原始 EA 通过识别长影线的拒绝形态
（针形线）并结合动量、较大周期的线性加权移动平均线 (LWMA) 趋势以及 MACD 方向过滤来寻找反转机会。
移植版本保留了这种多周期结构，完全依赖 StockSharp 指标，并将核心风控选项暴露为可调参数。

实现采用 StockSharp 的高级 API：主周期 K 线负责触发交易，而额外的订阅提供高周期指标数据。风险参数以点
(pips) 表示，同时支持可选的移动止损和保本移动逻辑。

## 入场条件
- **针形线检测**：上一根收盘的 K 线需具备至少 50% 的影线长度。
  - 多头信号：上影线占比达到阈值（对应原策略中的“上吊线”判定）。
  - 空头信号：下影线占比达到阈值。
- **趋势过滤**：高周期的快 LWMA（长度 = `FastMaPeriod`）必须高于/低于慢 LWMA（`SlowMaPeriod`）。
- **动量过滤**：最近三个高周期动量值与 100 的绝对偏离量中任一项需大于 `MomentumThreshold`。
- **MACD 过滤**：MACD 主线必须在对应周期上位于信号线上方/下方。
- **仓位限制**：净持仓量不得超过 `MaxTrades * Volume`，新增仓位使用对齐后的 `Volume`。

## 风险管理
- **止损 / 止盈**：按照 `StopLossPips` 和 `TakeProfitPips` 在入场价附近设置固定距离。
- **保本移动**：启用后，当价格至少运行 `BreakEvenTriggerPips` 时，将止损移动到 `入场价 ± BreakEvenOffsetPips`。
- **移动止损**：启用后，止损始终跟随距离最新收盘价 `TrailingStopPips` 的位置。
- **自动平仓**：触发上述任一价位后，通过市价单立即平掉全部仓位。

## 参数说明
| 参数 | 描述 |
| --- | --- |
| `TradeVolume` | 每次入场使用的成交量，会自动对齐品种的最小步长。 |
| `MaxTrades` | 同方向允许的最大累计手数（按成交量限制）。 |
| `StopLossPips` | 止损距离（点）。 |
| `TakeProfitPips` | 止盈距离（点）。 |
| `EnableTrailing` / `TrailingStopPips` | 是否启用移动止损及其距离。 |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | 保本移动触发阈值和偏移量。 |
| `FastMaPeriod` / `SlowMaPeriod` | 高周期快、慢 LWMA 的长度。 |
| `MomentumPeriod` / `MomentumThreshold` | 动量指标长度及与 100 的最小偏离。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD 快慢线和信号线的周期。 |
| `CandleType` | 主周期 K 线类型，用于针形线识别。 |
| `HigherCandleType` | 用于 LWMA 与动量过滤的高周期 K 线类型。 |
| `MacdCandleType` | 用于 MACD 过滤的 K 线类型。 |

## 与 MetaTrader 版本的差异
- 移除了按资金计算的止盈、移动止损以及权益保护，统一使用点值控制风险。
- 依赖图表对象的分形线确认逻辑被指标条件取代，更适合 StockSharp 环境。
- 删除了所有提示、邮件和推送通知，仅保留交易核心流程。

## 使用建议
1. 绑定策略到目标证券和组合，根据需要调整三个周期参数以实现多周期过滤。
2. 确认交易品种的价格步长与点值定义一致（默认回退为 0.0001）。
3. 启动策略后，系统会在 K 线收盘时自动管理止损、止盈、移动止损和保本移动。
4. 根据品种波动性调节动量和 LWMA 周期，以匹配行情特征。
