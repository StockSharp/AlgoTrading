# Scalper EMA Simple 策略

## 概览

**Scalper EMA Simple 策略** 源自 MetaTrader 专家顾问 `ScalperEMAEASimple`。该策略结合快/慢指数移动平均线、随机振荡指标以及平均趋向指数（ADX）过滤器，用于在现有趋势中的短暂回调中寻找入场机会。默认针对流动性好的外汇品种设计，但只要交易标的采用点值（pip）管理，该逻辑就同样适用。

实现完全基于 StockSharp 的高级 API，并且只对已完成的 K 线做出响应。所有指标值按顺序逐条计算，无需重新扫描历史数据，因此能够满足实时交易需求。

## 指标组合

- **快速 EMA (`FastEmaPeriod`)**：捕捉短期动量。
- **慢速 EMA (`SlowEmaPeriod`)**：界定当前趋势方向。
- **随机振荡器 (`StochasticLength`、`StochasticKPeriod`、`StochasticDPeriod`)**：监控接近超买/超卖区域时的动量反转。
- **平均趋向指数**：当 ADX 高于 `AdxThreshold` 时拒绝信号，避免在极端趋势中追高追低。

当随机振荡器的 %K 线重新上穿超卖阈值（做多）或下穿超买阈值（做空）时会产生动量确认。EMA 组合提供方向过滤，ADX 则确保只有在趋势放缓的回调阶段才允许入场。

## 入场条件

1. K 线收盘价位于慢速 EMA 的趋势一侧，并且快速 EMA 与该方向一致（多头需要 `fast > slow`，空头需要 `fast < slow`）。
2. 当前 K 线与慢速 EMA 的距离必须小于 K 线实体范围，并且比前三根 K 线的距离更小，从而复刻原版 EA 中的回调检测逻辑。
3. 必须满足烛体穿越快速 EMA，或快速 EMA 与慢速 EMA 发生交叉，用于触发突破信号。
4. 随机振荡器在最近 `ConditionWindowBars` 根 K 线内完成从极值区域的反向穿越，以确认动量。
5. ADX 低于 `AdxThreshold`，防止在波动率急剧增加时出手。
6. 同方向信号之间至少间隔 `SignalCooldownBars` 根 K 线。

当上述条件全部满足时，策略会先平掉反向仓位，再按检测到的方向下达市价单。

## 离场与风控

- 入场后立即按照 `StopLossPips`（根据标的点值换算为价格）设置初始止损。
- 未实现盈利达到 `TrailingActivationPips` 后，启动距离为 `TrailingDistancePips` 的跟踪止损。
- 出现反向信号时先行平仓，再考虑反向开仓。

所有保护单都通过 StockSharp 的 `SetStopLoss` 辅助函数维护，确保止损与当前持仓数量匹配。

## 参数说明

| 参数 | 说明 |
|------|------|
| `Volume` | 每次信号的基础交易量，若已有反向持仓会自动补单以实现完全反转。 |
| `FastEmaPeriod` / `SlowEmaPeriod` | 指数移动平均线的快慢周期。 |
| `StochasticLength`、`StochasticKPeriod`、`StochasticDPeriod` | 随机振荡器配置，保持与原 EA 一致。 |
| `StochasticOversold` / `StochasticOverbought` | 定义回调区域的超卖/超买阈值。 |
| `AdxThreshold` | 允许交易的 ADX 上限。 |
| `SignalCooldownBars` | 同向信号之间的最小间隔根数。 |
| `ConditionWindowBars` | 回调、EMA 突破与随机确认必须同时满足的时间窗口。 |
| `StopLossPips` | 初始止损距离（点）。 |
| `TrailingDistancePips` | 跟踪止损的固定距离（点）。 |
| `TrailingActivationPips` | 激活跟踪止损所需的最低盈利（点）。 |
| `CandleType` | 用于计算全部指标的 K 线类型，默认 5 分钟。 |

## 实现细节

- 点值换算依赖标的的 `PriceStep`。对于三位或五位小数报价，会额外乘以 10，贴近 MetaTrader 的常用定义。
- 仅处理已完成的 K 线，因此信号在每根 K 线收盘后产生。
- 通过保存最近一次回调、EMA 突破以及随机确认所在的索引来模拟原 EA 的窗口逻辑，无需遍历整段历史记录。

## 使用步骤

1. 将策略挂接到已配置证券和投资组合的 `Connector` 或 `Trader` 上。
2. 确认证券具备有效的 `PriceStep`，以便执行点值换算。
3. 根据标的波动率调节参数。默认慢速 EMA 为 740，与原 EA 保持一致；若市场节奏更快，可适度降低该值。
4. 启动策略。满足条件后会自动发送市价单并维护相关保护指令。

> **风险提示**：该移植策略仅用于学习研究。在实盘部署前务必进行充分的前向测试和风险评估。
