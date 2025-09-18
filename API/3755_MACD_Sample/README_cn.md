# MACD Sample 策略

该策略使用 StockSharp 高级 API 复刻 MetaTrader 4 中的 "MACD Sample" 智能交易系统。它仅在单一标的上双向交易：当 MACD 线在合适的零轴一侧穿越信号线且 26 周期 EMA 走势配合时入场。固定止盈与跟踪止损通过 `StartProtection` 模块实现，与原版 EA 的风险控制保持一致。

## 交易逻辑

1. 等待至少 100 根收盘完成的 K 线，保证 MACD 与 EMA 拥有足够历史数据。
2. 计算标准 MACD(12, 26, 9) 以及其信号线，并额外计算周期为 `TrendMaPeriod`（默认 26）的指数移动平均作为趋势过滤器。
3. **做多入场** —— 仅在当前无持仓时触发。MACD 必须位于零轴下方并向上穿越信号线，上一根 K 线中 MACD 低于信号线，当前绝对值超过 `MacdOpenLevel`（以价格点表示），同时趋势 EMA 向上。
4. **做空入场** —— 条件对称：MACD 位于零轴上方并向下穿越信号线，上一根 K 线 MACD 高于信号线，当前值超过 `MacdOpenLevel`，EMA 向下。
5. **平多** —— 当 MACD 在零轴上方重新跌破信号线且数值高于 `MacdCloseLevel` 时平仓，也可能提前被 `StartProtection` 的止盈或跟踪止损触发。
6. **平空** —— 当 MACD 在零轴下方重新上穿信号线且绝对值高于 `MacdCloseLevel` 时平仓，同样受保护模块控制。

策略始终最多持有一笔仓位，全部使用按 `Volume` 属性指定手数的市价单。所有“点数”参数会自动乘以标的的最小价位变动（PriceStep），从而模拟 MetaTrader 的 `Point` 定义。

## 参数说明

| 参数 | 描述 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `FastEmaPeriod` | MACD 的快 EMA 周期 | 12 | 可优化范围 6…18。
| `SlowEmaPeriod` | MACD 的慢 EMA 周期 | 26 | 可优化范围 20…32。
| `SignalPeriod` | MACD 信号线的 EMA 周期 | 9 | 可优化范围 5…13。
| `TrendMaPeriod` | 趋势过滤用 EMA 周期 | 26 | 可优化范围 20…40。
| `MacdOpenLevel` | 入场阈值（MACD 点数） | 3 | 对应 MT4 中的 `MACDOpenLevel`。
| `MacdCloseLevel` | 离场阈值（MACD 点数） | 2 | 对应 `MACDCloseLevel`。
| `TakeProfitPoints` | 止盈距离（价格点） | 50 | 设为 0 可关闭止盈。
| `TrailingStopPoints` | 跟踪止损距离（价格点） | 30 | 设为 0 可关闭跟踪止损。
| `CandleType` | 指标使用的 K 线类型 | 5 分钟时间框架 | 支持任意 StockSharp 蜡烛类型。

## 实现细节

- 通过 `BindEx` / `Bind` 将 MACD 与 EMA 绑定到蜡烛订阅，StockSharp 会自动提供已完成的指标值，无需手动缓存。
- 只有在 `IsFormedAndOnlineAndAllowTrading()` 返回真时才评估信号，避免在加载历史或离线状态下误下单。
- 所有以“点”为单位的阈值都会按 `Security.PriceStep` 缩放，精准复现 MetaTrader 的点值算法。
- `StartProtection` 把固定止盈和跟踪止损交给交易所侧保护单，可通过参数启用或关闭任一模块。
- 使用 `LogInfo` 记录每一次决策，方便与原始 EA 的回测报表进行对比验证。

## 使用建议

- 原策略主要针对外汇主流货币对的日内交易，建议从相同市场与时间框架入手，再按标的特性微调参数。
- 如果标的的最小跳动值较特殊，请确认 `Security.PriceStep` 已正确配置；否则系统会使用默认值 1.0。
- 需要组合层面风控时，可结合 StockSharp 的投资组合保护功能或外部资金管理模块。

## 标签

- 趋势跟随
- 动量
- MACD 交叉
- 日内交易（默认 5 分钟）
- 止盈与跟踪止损
