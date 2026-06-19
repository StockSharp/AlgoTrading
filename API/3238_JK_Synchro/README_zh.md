# JK 同步策略

## 概述

**JK 同步策略**是 MetaTrader 5 专家顾问 "JK synchro"（MQL 编号 2415）的 StockSharp 版本。原始 EA 统计最近若干根 K 线中收盘价低于开盘价以及收盘价高于开盘价的数量，并在优势方向开仓。本移植版本保留了该逻辑，同时提供类型安全的参数、内置风控以及 StockSharp 框架的日志工具。

## 交易逻辑

1. 按照 `CandleType` 参数订阅蜡烛数据，并只在蜡烛收盘后处理。
2. 维护长度为 `AnalysisPeriod` 的滑动窗口，对每根蜡烛执行：
   - 当 `Open > Close` 时增加**看跌计数**。
   - 当 `Open < Close` 时增加**看涨计数**。
   - 若 `Open == Close`（十字星）则忽略。
3. 当窗口填满后判断：
   - 如果看跌数量大于看涨数量，准备开多单。
   - 如果看涨数量大于看跌数量，准备开空单。
4. 在发出订单前必须满足以下条件：
   - 策略已连接并允许交易（`IsFormedAndOnlineAndAllowTrading`）。
   - 当前小时位于 `StartHour` 与 `EndHour`（包含）之间。
   - 距离上一次进场已超过 `PauseBetweenTradesSeconds` 指定的冷却时间。
   - 新增的手数不会使净头寸超过 `MaxPositions * OrderVolume`。
5. 当出现反向信号且当前持有相反仓位时，策略会先平仓并等待下一根蜡烛，再考虑反向开仓。
6. 止损、止盈与移动止损以点数（pip）表示，并根据品种的最小报价步长自动换算成价格距离。

## 风险控制

- **止损 / 止盈**：以点数配置，可选。头寸变化时重新计算，在每根收盘蜡烛上检查触发条件。
- **移动止损**：当 `TrailingStopPips` 与 `TrailingStepPips` 均大于 0 时启用。盈利超过 `TrailingStop + TrailingStep` 后，止损按照设定步长向盈利方向移动。
- **仓位上限**：净头寸绝对值不得超过 `MaxPositions * OrderVolume`。
- **入场冷却**：在 `OnPositionChanged` 中记录每次成交时间，确保在冷却时间结束前不会再次开仓。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OrderVolume` | 0.1 | 每次市价单的交易量。 |
| `MaxPositions` | 10 | 单方向最多允许的手数。 |
| `AnalysisPeriod` | 18 | 统计看涨/看跌蜡烛的数量。 |
| `PauseBetweenTradesSeconds` | 540 | 入场后的冷却时间（秒）。 |
| `StartHour` | 3 | 交易窗口起始小时（包含）。 |
| `EndHour` | 6 | 交易窗口结束小时（包含）。 |
| `StopLossPips` | 50 | 止损距离（点）。设为 0 关闭。 |
| `TakeProfitPips` | 150 | 止盈距离（点）。设为 0 关闭。 |
| `TrailingStopPips` | 15 | 移动止损距离（点）。设为 0 关闭移动止损。 |
| `TrailingStepPips` | 5 | 更新移动止损前所需的额外盈利（点）。启用移动止损时必须为正。 |
| `CandleType` | 15 分钟 | 用于计算的蜡烛数据源。 |

## 实现要点

- 全程使用 StockSharp 高级 API（`SubscribeCandles`、`.Bind`、`BuyMarket`、`SellMarket`）。
- 在 `OnPositionChanged` 中记录成交时间以实现与原 EA 相同的冷却机制。
- 根据 `Security.PriceStep` 与 `Security.Decimals` 自动计算点值，对 3 位或 5 位报价品种自动乘以 10。
- 在蜡烛收盘时根据最高价与最低价检查止损与止盈触发。
- 移动止损完全复刻原始逻辑：只有当盈利超过 `TrailingStop + TrailingStep` 时才开始移动，并且不会反向调整。

## 使用建议

1. 根据合约大小调整 `OrderVolume` 与 `MaxPositions`，以符合账户风险要求。
2. `AnalysisPeriod` 应与所选时间框架匹配，时间框架越短通常需要更长的窗口以降低噪声。
3. 调整 `StartHour` 与 `EndHour`，使其覆盖目标市场的活跃时段，例如欧盘时段的欧元货币对。
4. 回测不同的止损、止盈与移动止损组合，原 EA 在不同市场环境下会切换不同的风控配置。

## 与 MQL 版本的差异

- StockSharp 采用净头寸模型，切换方向时先平掉原有仓位；而 MetaTrader 版本允许同时持有多方向头寸（套保）。
- 参数管理与日志系统依赖 StockSharp，便于优化与界面展示。
- 移动止损在蜡烛收盘时评估，与其他 StockSharp 策略移植保持一致，避免对未完成蜡烛产生过早反应。

通过以上设置，您可以在 StockSharp 生态内直接运行、分析并优化 JK 同步策略。
