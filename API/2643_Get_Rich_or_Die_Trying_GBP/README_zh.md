# Get Rich or Die Trying GBP 策略
[English](README.md) | [Русский](README_ru.md)

该 StockSharp 策略复刻了 MetaTrader 专家顾问 “Get Rich or Die Trying GBP”。它关注伦敦与纽约交易时段重叠的活跃区间，并在 1 分钟 K 线上等待短暂的方向性失衡。算法统计最近 `CountBars` 根 K 线中开盘价高于收盘价的数量（原始代码称之为“上涨”）与开盘价低于收盘价的数量，当两者不一致时，策略会在所选时间窗口的前 5 分钟尝试反向进入较弱的一方。

系统一次仅持有一个仓位。每次开仓后都会强制等待 61 秒，既设有主要的固定止盈，也设有更紧的提前止盈目标，并在价格足够有利时按需启动跟踪止损。所有距离都以点（pip）为单位，并通过品种的最小价格步长换算（对 3 位和 5 位小数报价自动乘以 10），从而保持与 MT5 版本一致的逻辑。

## 细节

- **入场条件**：
  - **多头**：最近 `CountBars` 根 1 分钟 K 线中 `Open > Close` 的数量多于 `Open < Close`，当前时间处于 `22:00 + AdditionalHour` 或 `19:00 + AdditionalHour` 之后的前 5 分钟，没有持仓，并且满足 61 秒冷却时间。
  - **空头**：最近 `CountBars` 根 K 线中 `Open < Close` 的数量多于 `Open > Close`，并满足相同的时间和冷却限制。
- **方向**：可做多也可做空。
- **出场条件**：
  - 主要止盈 `TakeProfitPips` 和止损 `StopLossPips`。
  - 当浮动盈亏达到 `SecondaryTakeProfitPips` 时提前平仓。
  - 可选的跟踪止损：当价格突破 `TrailingStopPips + TrailingStepPips` 时生效，把止损移动至距离价格 `TrailingStopPips` 的位置，同时遵守跟踪步长。
- **止损/止盈**：固定止损、固定止盈、提前止盈以及可选的跟踪止损。
- **时间过滤**：仅在调整后的 19:00 和 22:00 之后的前 5 分钟内交易。
- **冷却**：每次开仓后至少等待 61 秒才能再次开仓。
- **默认参数**：
  - `StopLossPips` = 100
  - `TakeProfitPips` = 100
  - `SecondaryTakeProfitPips` = 40
  - `TrailingStopPips` = 30
  - `TrailingStepPips` = 5
  - `CountBars` = 18
  - `AdditionalHour` = 2
  - `MaxPositions` = 1000
  - `CandleType` = 1 分钟周期
- **说明**：
  - 为了兼容原始 EA，保留了 `MaxPositions` 参数，但此移植版同一时间只保持一个仓位。
  - 点值换算会自动识别 3 位和 5 位小数的外汇报价，并把最小价格步长乘以 10。
  - 跟踪止损逻辑与 MT5 相同：只有当价格同时超过跟踪距离和跟踪步长时才会移动止损。
