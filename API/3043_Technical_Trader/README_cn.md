# Technical Trader 策略
[English](README.md) | [Русский](README_ru.md)

Technical Trader 策略是将 `MQL/22304/Technical_trader.mq5` 中的 MetaTrader 专家顾问迁移到 StockSharp 高级 API 的结果。策略利用两条简单移动平均线与自适应的价格聚类探测器，寻找当前买卖价附近被频繁触及的价位。当聚类方向与快慢均线的交叉一致时才会开仓，同时通过基于价格步长的止损和止盈距离来复制原始 MQL 配置的风险控制。

## 概览
- **平台：** StockSharp 高级策略 API。
- **数据源：** 指定周期的 K 线以及订单簿快照，用于获取最新买价和卖价。
- **风格：** 跟随靠近行情的流动性聚类进行方向性突破交易。
- **与原始脚本的对应：** 均线参数、历史收盘价采样、聚类容差和下单手数均依据 MQL 版本实现。

## 交易逻辑
1. 订阅配置好的蜡烛数据，并计算 `FastMaPeriod` 与 `SlowMaPeriod` 两条简单移动平均线。
2. 维护一个长度为 `HistoryDepth` 的滑动窗口，存储最近的收盘价并保留三位小数，以模拟 `NormalizeDouble` 的行为。
3. 构建价格出现频次的直方图，挑选重复次数超过 `ResistanceThreshold` 的价位作为流动性聚类。
4. 通过订单簿订阅跟踪最新的买价和卖价，如果暂时没有报价则回退至当前蜡烛的收盘价。
5. 做多条件：
   - 快速 SMA 高于慢速 SMA。
   - 一个符合条件的聚类位于当前卖价之下，且距离不超过 `LevelTolerance`。
   - 当策略为空仓或持有空头时，买入足够的数量以平掉空头并建立基础多头仓位。
6. 做空条件与做多对称，使用位于买价之上的聚类，并要求快速 SMA 低于慢速 SMA。
7. 开仓后，根据合约 `PriceStep` 与 `StopLossPoints`、`TakeProfitPoints` 计算止损与止盈价位，完全对应 MQL 中 `_Point` 的处理方式。
8. 每根收盘蜡烛都会检查当前买卖价是否触及止损或止盈，并在触发时平仓。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `FastMaPeriod` | 快速 SMA 的周期，用于产生交叉信号。 | 25 |
| `SlowMaPeriod` | 慢速 SMA 的周期，用于趋势过滤。 | 30 |
| `StopLossPoints` | 止损距离（按价格步长计算，`PriceStep * StopLossPoints`）。 | 30 |
| `TakeProfitPoints` | 止盈距离（按价格步长计算，`PriceStep * TakeProfitPoints`）。 | 100 |
| `ResistanceThreshold` | 认定为聚类所需的最小重复次数。 | 15 |
| `HistoryDepth` | 聚类分析使用的历史蜡烛数量（针对黄金品种可设置为 100）。 | 500 |
| `LevelTolerance` | 当前买卖价到聚类价位的最大允许偏差。 | 0.0005 |
| `CandleType` | 策略处理的蜡烛类型或时间框架。 | 1 分钟周期 |

## 实现细节
- 订阅订单簿以捕捉实时最优买卖价，重现 MQL 策略依赖实时 Tick 的执行方式。
- 聚类统计遵循 StockSharp 转换规范，不使用 LINQ，并复用内部缓冲区。
- 止损与止盈在策略内部管理，因为 StockSharp 使用合成指令而非券商侧挂单。
- 示例代码同时绘制蜡烛、两条 SMA 以及成交记录，方便在测试环境中观察行为。

## 使用建议
- 在更大周期上可适当增加 `HistoryDepth`，以保持足够的样本数量。
- 对于最小报价单位较小的品种，建议收紧 `LevelTolerance` 以过滤无关价位。
- 在流动性较弱的市场中，可降低 `ResistanceThreshold` 以适应较少的重复次数。
- 策略的下单手数由基类 `Strategy` 的 `Volume` 属性决定，启动前请根据交易标的进行调整。
