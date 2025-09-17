# EMA LWMA RSI 策略

## 概述
**EMA LWMA RSI Strategy** 将 MetaTrader 专家顾问 “EMA LWMA RSI” 迁移到 StockSharp。策略比较两条使用相同价格类型（并可选向前平移）的均线，并通过 RSI 滤波确认动量。算法仅在指定周期的完整蜡烛形成后触发信号，而且始终只持有单一净持仓：在反向开仓前会先平掉已有仓位。止损和止盈距离以 pip 表示，并根据标的的最小价位自动换算。

## 交易逻辑
1. 计算指数移动平均线（EMA）和线性加权移动平均线（LWMA），两者拥有独立周期，但共享 `MaAppliedPrice`。当 `MaShift` 大于 0 时，两条均线都会通过 `Shift` 指标向前平移对应的柱数，以模拟 MetaTrader 的 `shift` 参数。
2. RSI 使用独立的 `RsiAppliedPrice` 计算，50 水平作为多空分界。
3. 当收到一根完结蜡烛时：
   - 若 EMA 从上方向上穿越 LWMA（上一根 EMA 高于 LWMA，本根 EMA 低于 LWMA），且 RSI > 50，则触发 **买入** 信号。
   - 若 EMA 从下方向下穿越 LWMA（上一根 EMA 低于 LWMA，本根 EMA 高于 LWMA），且 RSI < 50，则触发 **卖出** 信号。
4. 信号只会设置内部等待标志。若需要反向开仓，策略先调用 `ClosePosition()` 平掉当前持仓；待成交确认后，立即按标志方向发送市价单。这一流程忠实还原了原始 EA 在下单前等待成交确认的逻辑。
5. 通过 `StartProtection` 启动保护。若某个止损/止盈参数为 0，则对应腿被忽略，与 MQL 行为保持一致。

## 实现要点
- 价格类型完全对应 MetaTrader 选项（Close、Open、High、Low、Median、Typical、Weighted、Average）。加权价按 `(High + Low + 2 * Close) / 4` 计算，与 `PRICE_WEIGHTED` 相同。
- Pip 计算会在 3/5 位外汇品种上自动将 `PriceStep` 乘以 10，使 1 pip 等于 10 个最小价位。
- 通过高层 `SubscribeCandles` 订阅蜡烛，并使用 `Shift` 指标实现平移，无需手动维护历史缓冲。
- 通过布尔标志 `_pendingBuy`、`_pendingSell` 管理待执行订单，防止在上一笔指令尚未完成时重复下单，成交或持仓符合信号后自动清零。
- 图表助手在主图绘制蜡烛与两条均线，RSI 则显示在独立面板上，方便监控。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | `1 小时时间框架` | 用于分析的蜡烛序列。 |
| `StopLossPips` | `int` | `150` | 止损距离（pip），设为 `0` 则关闭。 |
| `TakeProfitPips` | `int` | `150` | 止盈距离（pip），设为 `0` 则关闭。 |
| `EmaPeriod` | `int` | `28` | EMA 周期。 |
| `LwmaPeriod` | `int` | `8` | LWMA 周期。 |
| `MaShift` | `int` | `0` | 均线向前平移的柱数。 |
| `RsiPeriod` | `int` | `14` | RSI 平滑周期。 |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | EMA/LWMA 使用的价格类型。 |
| `RsiAppliedPrice` | `AppliedPriceType` | `Weighted` | RSI 使用的价格类型。 |

## 使用步骤
1. 将策略绑定到目标证券，并将 `CandleType` 调整为与 MetaTrader 中相同的周期。
2. 根据经纪商需求修改 pip 保护距离及指标参数。
3. 启动订阅后允许交易。策略始终只持有单一方向仓位，并会先通过 `ClosePosition()` 平仓再反手。

当前暂未提供该策略的 Python 版本。
