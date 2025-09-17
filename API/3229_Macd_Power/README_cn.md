# MACD Power 策略

## 概览
MACD Power 是从 MetaTrader 智能交易顾问移植而来的多周期动量策略。算法在主周期上使用两条线性加权移动平均线（LWMA），结合两组 MACD 指标、高一级周期的动量过滤器以及月线级别的 MACD 趋势过滤器。当短期动量与长期趋势同时指向同一方向时，策略尝试跟随行情的强劲波动。

## 核心逻辑
- **典型价格 LWMA**：快速与慢速 LWMA 使用典型价格 \((High + Low + Close) / 3\) 计算。只有当快速 LWMA 位于慢速 LWMA 下方（与原始 EA 相同的前提条件）时才会评估交易信号。
- **双 MACD 过滤**：两组 MACD `(12, 26, 1)` 与 `(6, 13, 1)` 必须同时高于零（做多）或低于零（做空）。这正是 MQL 策略中 `MacdMAIN1` 与 `MacdMAIN2` 的判断，用于确认短期加速。
- **动量过滤器**：动量指标（周期 14）在比主周期更高的周期上计算，例如主图为 15 分钟，则动量在 1 小时级别计算。最近三个动量值与 100 的绝对偏差中至少有一个需要超过阈值 `MomentumBuyThreshold` 或 `MomentumSellThreshold`。
- **月线 MACD**：月线级别 `(12, 26, 9)` 的 MACD（对应 EA 中的 `MacdMAIN0`/`MacdSIGNAL0`）要求主线高于信号线才能做多，反之则做空，用于保证顺应长期趋势。

## 仓位管理
- **下单数量**：`OrderVolume` 控制基础下单量。如果需要反向开仓，策略会在市场单中自动加入反向持仓量，实现一次性反转。
- **止盈止损**：`TakeProfitPoints` 与 `StopLossPoints` 以品种点值表示，并通过 `Security.PriceStep` 转换成价格距离（当未提供步长时使用安全默认值 `1`）。
- **移动止损**：当浮盈达到 `TrailingActivationPoints` 时启用，止损保持在最高价（多头）或最低价（空头）附近，间距由 `TrailingOffsetPoints` 控制。
- **保本保护**：价格达到 `BreakEvenTriggerPoints` 后，策略会把保护位移动到“入场价 ± BreakEvenOffsetPoints”。如果价格回撤到该位置，立即平仓。
- **交易次数限制**：`MaxTrades` 用于限制本次运行内可开启的总交易次数。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 主周期蜡烛类型。 | 15 分钟 |
| `FastMaLength` | 快速 LWMA 周期（典型价格）。 | 6 |
| `SlowMaLength` | 慢速 LWMA 周期（典型价格）。 | 85 |
| `MomentumLength` | 高周期动量的计算长度。 | 14 |
| `MomentumBuyThreshold` | 做多时动量与 100 的最小偏差。 | 0.3 |
| `MomentumSellThreshold` | 做空时动量与 100 的最小偏差。 | 0.3 |
| `TakeProfitPoints` | 止盈距离（点）。 | 50 |
| `StopLossPoints` | 止损距离（点）。 | 20 |
| `TrailingActivationPoints` | 启动移动止损所需的盈利（点）。 | 40 |
| `TrailingOffsetPoints` | 移动止损与极值之间的距离（点）。 | 40 |
| `BreakEvenTriggerPoints` | 触发保本保护的盈利（点）。 | 30 |
| `BreakEvenOffsetPoints` | 保本时与入场价的偏移（点）。 | 30 |
| `MaxTrades` | 每次运行允许的最大交易次数。 | 10 |
| `OrderVolume` | 基础下单量。 | 1 |

## 与 MQL 版本的差异
- 实现使用 StockSharp 的高级 API（`SubscribeCandles`, `Bind`, `BindEx`），因此只在蜡烛收盘后处理指标，不再依赖逐笔循环。
- 原始代码中的资金止盈、权益回撤保护等功能未移植——在 StockSharp 生态中通常由独立的风险管理模块负责。策略保留了基于点值的止盈止损、移动止损和保本逻辑。
- 邮件、推送提醒以及手动修改挂单的辅助函数被移除，所有下单均通过市场指令完成。

## 使用建议
1. 通过 `CandleType` 选择主周期。更高周期的动量以及月线 MACD 会根据 `GetMomentumCandleType()` 中的映射自动确定。
2. 根据交易品种的最小变动价位调整 `TakeProfitPoints`、`StopLossPoints` 以及相关的风险参数。
3. 在回测或实盘中关注 `MaxTrades` 限制；若需要允许更多连续建仓，可适当调大该值。
4. 在图表界面启用显示时，策略会绘制主周期蜡烛与两条 LWMA，方便进行视觉分析。

