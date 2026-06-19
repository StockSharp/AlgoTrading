# Renko Fractals Grid 策略

## 概述
Renko Fractals Grid 源自 MetaTrader 4 的 "RENKO FRACTALS GRID" 专家顾问。策略在最近的比尔·威廉姆斯分形突破时入场，并结合 Renko 风格的波动过滤、加权移动平均的趋势判断以及基于 Rate of Change 的动量强度确认。移植到 StockSharp 后保留了原始机器人的网格式仓位管理：马丁格尔式加仓、无损移动、移动止损、权益保护以及可选的浮动利润拖尾。

## 交易逻辑
- **分形突破：** 多头需要最近的上方分形被最新收盘价突破，同时前三根 K 线中至少有一根收盘价低于该分形。空头条件与之相反。
- **Renko 过滤：** 检查最近 _CandlesToRetrace_ 根 K 线的高低点范围。只有当当前收盘价距离这些极值至少一个 Renko “砖块”（固定点数或最新 ATR 值）时信号才有效。
- **趋势过滤：** 快速与慢速加权移动平均线必须同向（多头时快速线在慢速线上方，空头反之）。
- **动量检查：** 最近三个 Rate of Change 值与 100 的绝对偏差需要大于设定阈值，以复刻 MQL4 中的 `iMomentum` 过滤器。
- **MACD 确认：** 仅当 MACD 主线位于信号线正确一侧时允许入场，同时该条件也用于提前离场。

## 风险管理
- **马丁格尔网格：** 每次加仓都会把基础手数乘以 _LotExponent_，同时持仓数量受 _MaxTrades_ 限制。
- **止损与止盈：** 根据平均持仓价加减固定点数计算。
- **无损移动：** 当盈利达到 _BreakEvenTriggerPips_ 时，止损上移到入场价并额外偏移 _BreakEvenOffsetPips_。
- **移动止损：** 以 K 线最高/最低价为基础，跟踪自入场以来的最大有利波动。
- **资金拖尾：** 可选的浮动利润管理，当利润达到 _MoneyTakeProfit_ 后，如果回撤超过 _MoneyStopLoss_ 则平掉所有仓位。
- **权益止损：** 记录组合价值与未实现盈亏形成的权益峰值，若回撤超过 _EquityRiskPercent_ 指定的百分比，则立即清仓。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 构建指标所用的主 K 线类型。 |
| `FastMaLength` / `SlowMaLength` | 加权移动平均的快慢周期。 |
| `MomentumLength` | Rate of Change 的回看长度。 |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | 触发多/空信号所需的最小绝对偏差。 |
| `UseAtrFilter` | 是否使用 ATR 作为 Renko 砖块大小。 |
| `BoxSizePips` | 当 ATR 过滤关闭时的固定砖块大小（点）。 |
| `CandlesToRetrace` | 用于扫描高低点的 K 线数量。 |
| `BaseVolume` | 初始下单手数。 |
| `LotExponent` | 每次加仓的手数倍数。 |
| `MaxTrades` | 同方向允许的最大仓位数量。 |
| `StopLossPips` / `TakeProfitPips` | 固定止损与止盈距离（点）。 |
| `TrailingStopPips` | 移动止损距离（点，0 表示关闭）。 |
| `UseBreakEven` | 是否启用无损移动。 |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | 启动无损移动所需的点数及额外偏移。 |
| `UseMoneyTarget` | 是否启用浮动利润拖尾。 |
| `MoneyTakeProfit` / `MoneyStopLoss` | 激活资金拖尾的利润阈值与允许回撤。 |
| `UseEquityStop` | 是否启用权益止损。 |
| `EquityRiskPercent` | 允许的权益峰值回撤百分比。 |

## 实现说明
- 原版 EA 在月线级别计算 MACD。移植版在工作时间框架上使用相同参数，因为默认情况下没有额外的时间框架数据。
- 所有来自 “点” 的价格偏移都会依据合约最小报价步长转换，从而兼容五位数报价。
- 已通过成交事件估算实现盈亏，从而在缺少账户统计数据时仍可执行权益回撤监控。
- 策略采用高阶蜡烛订阅与指标绑定，且源码中的注释均使用英文，以满足项目要求。
