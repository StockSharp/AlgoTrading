# RRS Impulse 策略

**RRS Impulse** 是 MetaTrader 专家顾问“RRS Impulse”的 StockSharp 高级 API 版本。原始 EA 结合了 RSI、随机指标和布林带，
可以切换多种信号强度模式，并使用虚拟止损/移动止损。本移植在 C# 中复刻了这些特点：通过蜡烛订阅驱动指标，交易指令全部通过
`BuyMarket`、`SellMarket` 与 `ClosePosition` 等高级方法完成。

## 交易逻辑

1. **指标模式** – 四种选择：
   - `Rsi`：当 RSI 离开超买/超卖区域时入场。
   - `Stochastic`：要求 %K 与 %D 同时位于设定阈值之上或之下。
   - `BollingerBands`：收盘价高于上轨或低于下轨时触发。
   - `RsiStochasticBollinger`：三项过滤器全部一致时才允许下单。
2. **交易方向** – `Trend` 顺势操作（超买做空、超卖做多），`CounterTrend` 则反向博弈。
3. **信号强度** – 决定需要多少时间框架同时满足条件：
   - `SingleTimeFrame`：只检查基础时间框架 `CandleType`。
   - `MultiTimeFrame`：需要 M1、M5、M15、M30、H1、H4 全部同向。
   - `Strong`：侧重日内动量（M1、M5、M15、M30）。
   - `VeryStrong`：同样使用 M1…H4 全阶梯。若启用复合指标模式，则每个时间框架都必须同时满足 RSI、随机指标与布林带条件。
4. **风险控制** – 每笔仓位都会跟踪三类虚拟保护：
   - 固定止损点数；
   - 固定止盈点数；
   - 当浮动盈利超过 `TrailingStartPips` 时启动的移动止损，跟随距离由 `TrailingGapPips` 定义。
   当方向反转时，策略先调用 `ClosePosition()` 平掉现有仓位，下一个确认信号到来后再考虑开立反向单。

## 参数

| 分类        | 名称 | 说明 |
|-------------|------|------|
| Data        | `CandleType` | 用于决策的基础蜡烛序列。 |
| Orders      | `TradeVolume` | 下单的合约数量/手数。 |
| Risk        | `StopLossPips`, `TakeProfitPips`, `TrailingStartPips`, `TrailingGapPips` | 以点数表示的虚拟保护。 |
| Signals     | `IndicatorMode`, `TradeDirection`, `SignalStrength` | 复制自 EA 输入参数的行为开关。 |
| RSI         | `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | RSI 计算周期及阈值。 |
| Stochastic  | `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing`, `StochasticUpperLevel`, `StochasticLowerLevel` | 慢速随机指标设置。 |
| Bollinger   | `BollingerPeriod`, `BollingerDeviation` | 布林带长度与标准差倍数。 |

与原版相同，关键参数保留了可优化范围，例如止损/止盈距离和振荡指标阈值。

## 数据需求

策略需要分钟级别的历史蜡烛。当 `SignalStrength` 选择更严格的模式时，程序会自动订阅所需的多个时间框架
（`GetWorkingSecurities` 会向引擎声明这些需求）。策略不依赖 Level1 行情，所有决策仅基于完成的蜡烛收盘价，因而完全复现了
MetaTrader 中的“虚拟”止损与止盈。

## 移植说明

- 原 EA 中随机切换交易品种的逻辑已移除。StockSharp 策略针对单一 `Security` 运行，品种选择交由用户配置。
- 持仓管理完全采用市价指令：当出现反向信号或保护条件触发时，先执行 `ClosePosition()`，对应 MQL 中遍历订单并逐一关闭的流程。
- 代码注释全部使用英文，缩进使用制表符，完全符合仓库提供的贡献规范。
