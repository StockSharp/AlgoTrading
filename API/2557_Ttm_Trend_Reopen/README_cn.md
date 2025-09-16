# TTM 趋势再入场策略

## 概述
该策略复刻了 MetaTrader 智能交易系统 *Exp_ttm-trend_ReOpen* 的核心逻辑。它在 StockSharp 框架中重建 TTM Trend 指标，使用 Heikin-Ashi 平滑来给蜡烛着色，并在颜色从看跌翻转为看涨或反向时执行交易。每一次颜色改变都代表波动压缩或扩张状态的切换，策略会立即平掉反向持仓并按新的方向入场。

## 指标机制
原始指标根据 Heikin-Ashi 实体与传统 K 线的组合情况给蜡烛着色：

- **亮绿色 (4)** – Heikin-Ashi 收盘高于开盘，且标准 K 线收阳。
- **青绿色 (3)** – Heikin-Ashi 看涨，但标准 K 线收阴。
- **洋红色 (0)** – Heikin-Ashi 看跌，且标准 K 线收阴。
- **紫色 (1)** – Heikin-Ashi 看跌，而标准 K 线收阳。
- **灰色 (2)** – 无法判断趋势时的默认颜色。

为了模拟 MetaTrader 中的平滑逻辑，指标会维护一个 `CompBars` 长度的 Heikin-Ashi 历史窗口。如果最新实体完全落在任意历史实体的高低范围内，则沿用历史颜色，从而过滤掉细小回撤所产生的噪声，这与原始实现保持一致。

## 交易规则
1. 订阅 `CandleType` 指定的周期，只评估已经收盘的蜡烛；`SignalBar` 用于指定相对于最新历史点回看多少根已收盘蜡烛。
2. 当出现 **看涨颜色**（值为 1 或 4）且上一信号并非看涨时：
   - 若启用 `EnableShortExits`，先平掉空头仓位。
   - 若启用 `EnableLongEntries`，开多或从空头翻多。
3. 当出现 **看跌颜色**（值为 0 或 3）且上一信号并非看跌时：
   - 若启用 `EnableLongExits`，先平掉多头仓位。
   - 若启用 `EnableShortEntries`，开空或从多头翻空。
4. 当浮盈达到 `PriceStepPoints`（根据标的 `PriceStep` 转换为价格）时，策略可以按当前方向继续加仓。每个方向的累计入场次数由 `MaxPositions` 限制。

## 加仓逻辑
- `PriceStepPoints` 对应原版 EA 的“PriceStep”参数：当未实现利润超过这一距离时，再加一笔基础 `Volume`。
- `MaxPositions` 定义每个方向最多允许的仓位次数（包含首笔）。若设为 `1`，即完全关闭再入场功能。

## 风险控制
`StopLossPoints` 与 `TakeProfitPoints` 以标的点值表示，与原 EA 一致。策略会根据 `Security.PriceStep` 将其换算为绝对价格距离，并通过 `StartProtection` 自动挂出止损/止盈。将任一参数设为 0 即可禁用对应保护。

## 参数说明
- `CandleType` – 计算 TTM Trend 时使用的时间周期（默认 4 小时）。
- `CompBars` – 用于平滑颜色的 Heikin-Ashi 历史长度（默认 6）。
- `SignalBar` – 相对最新完成蜡烛回看多少根做决策（默认 1，即最近一根收盘蜡烛）。
- `PriceStepPoints` – 触发加仓所需的最小盈利点数（默认 300）。
- `MaxPositions` – 每个方向的累计开仓上限（默认 10）。
- `EnableLongEntries` / `EnableShortEntries` – 控制颜色翻转时是否开多/开空。
- `EnableLongExits` / `EnableShortExits` – 控制出现反向颜色时是否强制平仓。
- `StopLossPoints` – 止损距离（默认 1000 点）。
- `TakeProfitPoints` – 止盈距离（默认 2000 点）。

## 使用建议
- TTM Trend 对时间周期较敏感；原系统使用 H4 图表，但本策略可接入任何 `CandleType`。
- 指标基于 Heikin-Ashi 实体，跳空行情可能需要下一根蜡烛确认颜色翻转。
- 若不希望加仓，将 `PriceStepPoints` 设为 0 即可实现单次入场模式。
