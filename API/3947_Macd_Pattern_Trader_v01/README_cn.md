# MacdPatternTraderv01 策略

## 概览

`MacdPatternTraderV01Strategy` 是 FORTRADER 杂志的 MetaTrader4 智能交易程序 "MacdPatternTraderv01" 在 StockSharp 平台上的完整移植版本。策略识别 MACD 指标在极值位置形成的“钩形”形态：当 MACD 远离零轴后再次向零轴回拉并出现反向拐点时，系统立即跟随预期的反转方向开仓。代码不仅复现了信号逻辑，还保留了递归止盈、止损和分批减仓等风险控制机制。

实现基于高层 `SubscribeCandles` 接口，通过 `MACD`、`ExponentialMovingAverage` 与 `SimpleMovingAverage` 指标处理收盘后的完整 K 线。MACD 的历史值使用三个元素的滑动寄存器保存，从而模拟 MQL 中 `iMACD(..., shift)` 的访问方式。高低点缓冲区上限为 1000 条，足以覆盖默认参数，同时避免内存无限增长。

## 入场条件

1. **准备做空**：当 MACD 主线高于 `BearishThreshold` 时，记录潜在的做空机会；若 MACD 跌破 0 则取消。
2. **准备做多**：当 MACD 主线低于 `BullishThreshold` 时，记录潜在的做多机会；若 MACD 升回 0 以上则取消。
3. **钩形确认**：
   - 做空需要满足 `macd₀ < BearishThreshold`、`macd₀ < macd₁`、`macd₁ > macd₂`，且历史极值仍高于阈值，同时最新 MACD 仍为正值。
   - 做多需要满足 `macd₀ > BullishThreshold`、`macd₀ > macd₁`、`macd₁ < macd₂`，且历史极值仍低于阈值，同时最新 MACD 仍为负值。
4. **下单执行**：当形态确认后，按 `OrderVolume` 发送市价单，并立即记录对应的止损、止盈价格以便后续追踪。

## 风险管理

### 止损

- 空单：从最近 `StopLossBars` 根已完成 K 线（不含当前）中寻找最高价，并加上 `OffsetPoints * PriceStep`。
- 多单：从同样数量的历史 K 线中寻找最低价，并减去相同的偏移量。

### 止盈

止盈沿用原策略的递归搜索：

1. 以 `TakeProfitBars` 为窗口，包含当前完成的 K 线，计算极值。
2. 向更早的区间移动 `TakeProfitBars` 根 K 线，若新极值更有利则继续。
3. 当新窗口不再改进极值时停止，使用最后一次更新的价格作为止盈水平。

### 分批减仓

- 系统记录初始仓位与入场价，只有当浮动利润（以账户货币计）大于 `ProfitThreshold` 时才允许分批平仓。
- 多单：当 K 线收盘价高于 `EmaMediumPeriod` 时平掉三分之一仓位；当最高价突破 `(SMA + EMA Long)/2` 时再平掉剩余仓位的一半。
- 空单：规则对称，要求收盘价低于中期 EMA，最低价跌破相同的均线组合。
- 每根 K 线先检查是否触发硬性止损或止盈，再评估分批退出，确保安全规则优先执行。

## 参数列表

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `StopLossBars` | 6 | 计算止损所需的历史 K 线数量。 |
| `TakeProfitBars` | 20 | 止盈递归算法的窗口长度。 |
| `OffsetPoints` | 10 | 止损价格的额外点数偏移。 |
| `MacdFastPeriod` | 5 | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | 13 | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | 1 | MACD 信号 EMA 周期。 |
| `BearishThreshold` | 0.0045 | 做空准备的正向 MACD 阈值。 |
| `BullishThreshold` | -0.0045 | 做多准备的负向 MACD 阈值。 |
| `OrderVolume` | 1 | 每次市价单的交易量。 |
| `EmaShortPeriod` | 7 | 首次减仓条件所用的 EMA 周期。 |
| `EmaMediumPeriod` | 21 | 过滤和首次减仓共用的 EMA 周期。 |
| `SmaPeriod` | 98 | 与长 EMA 组合使用的 SMA 周期。 |
| `EmaLongPeriod` | 365 | 参与复合均线的长 EMA 周期。 |
| `ProfitThreshold` | 5 | 触发减仓所需的最低浮动利润（账户货币）。 |
| `CandleType` | 1 小时 | 策略处理的蜡烛时间框架。 |

## 实现细节

- 所有参数均通过 `StrategyParam<T>` 暴露，可在优化器中直接调整。
- 保护价位以 `decimal` 存储，当某根 K 线触及止损或止盈时，策略会以市价一次性平仓并重置内部状态。
- `CalculateFloatingProfit` 根据 `PriceStep` 与 `StepPrice` 计算利润，使阈值在不同标的上保持一致。
- 代码中加入了大量英文注释，说明每个从 MQL 移植而来的逻辑模块，方便继续维护或扩展。

## 使用步骤

1. 创建 `MacdPatternTraderV01Strategy` 实例，设置交易的证券、组合和连接器。
2. 根据需求调整 `CandleType`、`StopLossBars`、`OrderVolume` 等参数。
3. 启动策略，系统会自动订阅蜡烛数据、绘制 MACD 与交易标记，并执行原版 EA 的全部下单与风控流程。
