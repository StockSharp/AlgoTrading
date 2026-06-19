# Ichimoku Barabashkakvn 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 平台上重现了 Vladimir Karputov（barabashkakvn 版本）的 Ichimoku 专家顾问。核心是 Tenkan/Kijun 的交叉配合云层位置，同时保留原始 EA 中的全部风控逻辑。

## 策略原理

- **指标组合**：使用一个 Ichimoku Kinko Hyo 指标（默认周期 9/26/52），获取 Tenkan、Kijun、Senkou Span A、Senkou Span B。
- **做多条件**：Tenkan 上穿 Kijun，且收盘价高于 Senkou Span B。交叉检测使用上一根 Tenkan 值，与原始 MQL 实现一致。
- **做空条件**：Tenkan 下穿 Kijun，且收盘价低于 Senkou Span A。
- **持仓管理**：始终只保留一个净头寸；反向信号会先平掉现有仓位，与 EA 的两步反转流程相同。
- **交易时段**：可选的时间过滤器，按开始/结束小时（包含端点）限制交易，判定方式复制自原代码。

## 风险控制

- **独立止损/止盈**：多空分别设置止损与止盈点数，使用品种的最小价格跳动，并对 3 位和 5 位小数报价乘以 10，完全复刻 EA 的点值换算。
- **移动止损**：多空各自拥有独立的移动距离，并共享一个“步长”。只有当浮盈超过“距离 + 步长”后才会上移/下移止损，行为与原脚本相同。
- **保护执行**：每根完结 K 线都会检查虚拟止损/止盈，以模拟 MetaTrader 上的服务器委托效果。

## 参数

- `TenkanPeriod`（默认 9）– Tenkan 周期。
- `KijunPeriod`（26）– Kijun 周期。
- `SenkouSpanBPeriod`（52）– Senkou Span B 周期。
- `CandleType`（1 小时）– 计算所用的 K 线类型。
- `OrderVolume`（1 手）– 交易手数。
- `BuyStopLossPips` / `SellStopLossPips`（100）– 多/空止损点数。
- `BuyTakeProfitPips` / `SellTakeProfitPips`（300）– 多/空止盈点数。
- `BuyTrailingStopPips` / `SellTrailingStopPips`（50）– 多/空移动止损距离。
- `TrailingStepPips`（5）– 调整移动止损所需的额外盈利点数。
- `UseTradeHours`（false）– 是否启用时间过滤。
- `StartHour` / `EndHour`（0 / 23）– 允许交易的小时范围，0–23 之间。

所有参数均通过 `StrategyParam<T>` 暴露，可在 StockSharp Designer 中直接调整或优化，无需修改源码。
