# No Nonsense Tester 策略

## 概述
**No Nonsense Tester 策略** 是 MQL4 "NoNonsenseTester" 智能交易系统在 StockSharp 平台上的移植版本。实现遵循 NNFX 的核心流程：先确认趋势基准线，再等待两个确认指标，使用 ATR 检查波动性，并通过严格的退出规则管理头寸。策略全部关键阈值都通过 `StrategyParam` 参数公开，方便在 StockSharp 中进行批量优化测试。

## 交易逻辑
1. **基准线过滤**：可配置周期的 EMA 定义主要趋势，仅在收盘价穿越基准线时考虑入场。
2. **确认指标一**：RSI 必须位于多头阈值之上或空头互补阈值之下，用于验证基准线突破。
3. **确认指标二**：CCI 需与趋势方向一致，并超过设定的绝对幅度，以过滤掉噪音信号。
4. **波动性过滤**：ATR 必须高于 `AtrMinimum`，确保仅在具备足够波动的市场环境中开仓。
5. **开仓**：当基准线穿越、两项确认和波动性条件全部满足时，策略按突破方向建仓。`AtrEntryMultiplier` 参数允许按 ATR 规模调整下单量。
6. **止盈止损**：建仓后立刻计算 ATR 倍数的止损与止盈。若启用 ATR 拖尾，则在行情向有利方向发展时持续上调保护止损。
7. **退出覆盖**：额外的短周期 RSI 监控持仓，一旦多头跌破下轨或空头突破上轨，即使价格尚未触及保护位也会提前离场。

## 参数说明
| 参数 | 说明 |
|------|------|
| `BaselineLength` | EMA 基准线周期。 |
| `ConfirmationRsiLength` | RSI 确认指标周期。 |
| `ConfirmationRsiThreshold` | RSI 多空分界阈值。 |
| `ConfirmationCciLength` | CCI 确认指标周期。 |
| `ConfirmationCciThreshold` | 接受信号所需的 CCI 绝对幅度。 |
| `AtrPeriod` | ATR 计算周期。 |
| `AtrEntryMultiplier` | 按 ATR 调整下单量的倍数。 |
| `AtrTakeProfitMultiplier` | 止盈使用的 ATR 倍数。 |
| `AtrStopLossMultiplier` | 止损使用的 ATR 倍数。 |
| `AtrTrailingMultiplier` | 拖尾止损的 ATR 倍数，设为 `0` 表示关闭。 |
| `AtrMinimum` | 开仓所需的最小 ATR。 |
| `ExitRsiLength` | 退出 RSI 的周期。 |
| `ExitRsiUpperLevel` | 触发空头离场的 RSI 上轨。 |
| `ExitRsiLowerLevel` | 触发多头离场的 RSI 下轨。 |
| `CandleType` | 计算使用的蜡烛类型（时间框架）。 |

## 图表元素
策略会自动绘制：
- 原始蜡烛。
- EMA 基准线。
- 成交记录标记。

## 优化建议
所有核心 `StrategyParam` 参数都设置了优化范围，延续原版测试器的可调性。可通过 StockSharp 优化工具扫描不同的基准线周期、确认阈值以及风险设置，复现 MQL 版本的参数网格测试。

## 使用提示
- 根据个人 NNFX 指标组合调整阈值，快速验证自定义模板。
- 合理设置 `AtrMinimum`，避免在低波动区间频繁交易。
- 若要测试续仓策略，可将 `AtrTrailingMultiplier` 设为大于零，让盈利头寸在保护止损的同时继续扩展。

