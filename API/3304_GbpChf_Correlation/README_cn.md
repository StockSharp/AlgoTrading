# GBPCHF 相关性策略
[English](README.md) | [Русский](README_ru.md)

**GBPCHF 相关性策略** 使用 StockSharp 高级 API 重现 MetaTrader 专家顾问 “GbpChf 4”。策略在 1 小时 K 线级别跟踪 GBPUSD 与 USDCHF 的联动关系，当两条货币腿在多头或空头方向上形成一致动能时，会在指定品种（通常是 GBPCHF）上开仓。

## 工作机制
- 对 GBPUSD 和 USDCHF 的 1 小时 K 线分别构建 MACD(12/26/9) 指标。
- 同时评估 MACD 柱状图（动量）与信号线（趋势确认）。
- 当两条柱状图均为正值、GBPUSD 柱状图弱于 USDCHF 且 GBPUSD 信号线高于 USDCHF 信号线时出现做多信号。
- 做空信号要求两条柱状图均为负值、GBPUSD 柱状图跌势强于 USDCHF，且 GBPUSD 信号线低于 USDCHF 信号线。
- 每根 K 线最多生成一个方向的订单，可选的净头寸限制使策略在已有仓位未平仓前不会再次开仓。

## 风险控制
- 止损与止盈距离以点（pip）表示，并通过交易品种的 `PriceStep` 转换为绝对价格距离。
- 通过 `StartProtection` 启动自动保护，即使策略离线，券商/服务器也会继续执行止损或止盈。

## 参数说明
- `Volume` – 每次信号的下单量，默认 `0.01`。
- `StopLossPips` – 止损距离（点），默认 `90`。
- `TakeProfitPips` – 止盈距离（点），默认 `45`。
- `OnlyOnePosition` – 设为 `true` 时，只有在净头寸为零时才允许开新仓。
- `FastPeriod` – MACD 快速 EMA 周期，默认 `12`。
- `SlowPeriod` – MACD 慢速 EMA 周期，默认 `26`。
- `SignalPeriod` – MACD 信号线的 SMA 周期，默认 `9`。
- `CandleType` – 所有订阅使用的时间框架，默认 1 小时。
- `GbpUsdSymbol` – GBPUSD 品种的标识符。
- `UsdChfSymbol` – USDCHF 品种的标识符。

## 使用提示
- 需要保证 GBPUSD 与 USDCHF 数据源同步，且拥有完整的小时 K 线。
- 策略会在日志中记录订单取消与成交情况，便于确认相关性逻辑是否生效。
- 在发送新的市价单前会取消挂单，确保持仓切换更干净。
- 连接器必须能够解析 GBPUSD 与 USDCHF 品种（例如通过符号映射或行情查询）。

## 默认设置摘要
- **时间框架**：1 小时 K 线。
- **交易方向**：多头与空头。
- **涉及品种**：在配置的 `Security`（默认 GBPCHF）上交易，分析 GBPUSD 与 USDCHF。
- **止损/止盈**：支持（固定点数距离）。
- **指标**：GBPUSD 与 USDCHF 上的 MACD 柱状图与信号线。
- **复杂度**：中等（多品种相关性 + 风险管理）。
