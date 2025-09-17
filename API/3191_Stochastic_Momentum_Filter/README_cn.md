# 随机动量过滤策略

## 概述
**Stochastic Momentum Filter Strategy** 是 MetaTrader 专家顾问 `Stochastic.mq4`（位于 `MQL/23473`）的 StockSharp 版本。原始 EA 同时使用两个随机指标、线性加权移动平均线（LWMA）、动量偏离过滤器以及高时间框 MACD。本 C# 实现依托 StockSharp 的高级 API 重建上述组件，并保留原策略的多重确认流程：

1. **趋势过滤**：只有当快速 LWMA 位于慢速 LWMA 之上（或之下）时才允许开多（或开空）。
2. **震荡确认**：快速随机指标（5/2/2）与慢速随机指标（21/4/10）必须同时给出超买/超卖信号。
3. **动量偏离**：最近三根 K 线的动量值必须至少有一个偏离 100 基准超过给定阈值，对应 MQL 版本使用的 `iMomentum` 指标。
4. **高周期 MACD**：在可配置的高时间框上，MACD 主线需要位于信号线上方（做多）或下方（做空）。默认的 30 天时间框近似原策略的月线过滤。
5. **风险控制**：通过 `StartProtection` 设置止损、止盈以及可选的跟踪止损，与原 EA 的保护机制相呼应。方向反转时，策略会先平掉反向持仓再建立新的净头寸。

策略会同时订阅两个蜡烛流：交易时间框和高时间框。所有计算均基于 StockSharp 指标完成，并借助 `Bind` 系列高阶方法处理。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `StochasticBuyLevel` | `30` | 两个随机指标必须跌破的超卖阈值。 |
| `StochasticSellLevel` | `80` | 两个随机指标必须升破的超买阈值。 |
| `FastMaPeriod` | `6` | 快速 LWMA 的周期。 |
| `SlowMaPeriod` | `85` | 慢速 LWMA 的周期。 |
| `FastStochasticPeriod` | `5` | 快速随机指标 `%K` 的周期。 |
| `FastStochasticSignal` | `2` | 快速随机指标 `%D` 的平滑周期。 |
| `FastStochasticSmoothing` | `2` | 快速随机指标的附加平滑系数（对应 MT4 的 Slowing 参数）。 |
| `SlowStochasticPeriod` | `21` | 慢速随机指标 `%K` 的周期。 |
| `SlowStochasticSignal` | `4` | 慢速随机指标 `%D` 的平滑周期。 |
| `SlowStochasticSmoothing` | `10` | 慢速随机指标的附加平滑系数。 |
| `MomentumPeriod` | `14` | 动量指标的回看周期（与 `iMomentum` 相同）。 |
| `MomentumThreshold` | `0.3` | 最近三次动量值中至少一个需要超过的绝对偏离阈值。 |
| `MacdFastPeriod` | `12` | 高时间框 MACD 的快 EMA 周期。 |
| `MacdSlowPeriod` | `26` | 高时间框 MACD 的慢 EMA 周期。 |
| `MacdSignalPeriod` | `9` | 高时间框 MACD 的信号 EMA 周期。 |
| `TakeProfitPoints` | `50` | 止盈距离（价格点）。设置为 `0` 可关闭。 |
| `StopLossPoints` | `20` | 止损距离（价格点）。设置为 `0` 可关闭。 |
| `EnableTrailing` | `true` | 是否启用跟踪止损。 |
| `TradeVolume` | `1` | 每次信号期望的净头寸大小。 |
| `MaxNetPositions` | `1` | 净头寸最大倍数（乘以 `TradeVolume`）。 |
| `CandleType` | `15m` | 交易时间框。 |
| `HigherTimeframe` | `30d` | MACD 使用的高时间框。 |

## 交易逻辑
1. **指标绑定**：将两条 LWMA、两个随机指标、动量指标和 MACD 分别绑定到对应的蜡烛订阅。
2. **动量缓存**：记录最近三根完成蜡烛的动量绝对偏离量，重现 EA 中的 `MomLevelB/MomLevelS` 逻辑。
3. **入场规则**
   - **做多**：快速 LWMA 高于慢速 LWMA，两个随机指标的 `%K` 与 `%D` 均低于 `StochasticBuyLevel`，动量偏离超过 `MomentumThreshold`，并且 MACD 主线位于信号线上方。
   - **做空**：快速 LWMA 低于慢速 LWMA，两个随机指标的 `%K` 与 `%D` 均高于 `StochasticSellLevel`，动量偏离超过阈值，且 MACD 主线位于信号线下方。
4. **仓位处理**：使用 `BuyMarket` / `SellMarket` 发送市价单，如方向反转，先平掉反向净仓再建立新仓。
5. **保护措施**：`StartProtection` 根据设置的点值应用止损/止盈。当 `EnableTrailing` 为真时，StockSharp 会自动跟踪止损，与 MQL 版本的 trailing 逻辑一致。

## 与 MQL 版本的差异
- **仓位放大**：原 EA 通过 `LotExponent` 叠加多个订单。本移植版本以净头寸为核心，通过 `TradeVolume` 和 `MaxNetPositions` 控制仓位规模。
- **保证金控制**：MT4 专有的保证金检查、权益止损和通知功能未迁移，因为它们依赖终端特定接口。
- **冻结区间**：经纪商的 freeze-level 检查交由 StockSharp 连接器处理，不在策略中重复实现。
- **保本功能**：原脚本的“移动到保本”由 `StartProtection` 的跟踪止损取代。

## 使用建议
1. 先分配交易品种和连接器，启动策略后会自动订阅所需的两个时间框。
2. 如果数据源不支持 30 天蜡烛，可将 `HigherTimeframe` 调整为周线、日线等可用周期。核心判定仍基于 MACD 主线与信号线的相对位置。
3. 按账户规模设置 `TradeVolume`，策略在 `OnStarted` 中会同步更新 `Volume` 字段，Designer/Runner 将使用该值下单。
4. 若不需要止损/止盈，请将 `StopLossPoints` 与 `TakeProfitPoints` 设为 0。
5. 源码内全部注释均为英文，缩进采用制表符，符合仓库规范。

## 文件结构
- `CS/StochasticMomentumFilterStrategy.cs` — 策略代码。
- `README.md` — 英文说明。
- `README_ru.md` — 俄文说明。
- `README_cn.md` — 中文说明（本文件）。
