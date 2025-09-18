# NNFX Auto Trade 策略

## 概述
**NNFX Auto Trade Strategy** 将原始 NNFX MetaTrader 4 面板移植到 StockSharp，通过参数而不是图形界面执行手动命令。交易者可以触发多空入场、立即平仓或单次执行保本与拖尾规则，完全遵循原策略的风险管理流程。

主要特点：

- 基于 ATR 的波动性头寸控制，同时支持自定义固定的止损与止盈距离。
- 入场仓位会拆分为两部分：第一部分在到达目标时落袋，第二部分留在市场中供交易者继续管理。
- 保本与拖尾命令按需执行，不会在每根 K 线上自动触发。
- 可在风险计算中加入外部资本，复现 MQL 版本的设置。

## 交易流程
1. **ATR 计算** – 订阅选定的 K 线类型并计算 Average True Range。当 `UsePreviousDailyAtr` 打开时，新交易日的前 12 小时内沿用上一日的 ATR 值。
2. **风险头寸** – 当 `Buy` 或 `Sell` 命令被触发时，根据止损距离计算单手风险，并将设定的风险百分比转换为交易量。
3. **仓位拆分** – 入场数量被平均分成两份，一份在达到目标价时平仓，另一份保持持仓。
4. **止损管理** – 初始止损保存在内部字段，每根已完成的 K 线都会检查是否触发，同时可以通过命令移动到保本或按照 NNFX 公式更新拖尾。
5. **退出控制** – `CloseAll` 会立即平掉所有仓位；若价格触及止损或目标，策略会按计算的数量下市价单离场。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `RiskPercent` | `2.0` | 每笔交易允许承担的权益百分比（含 `AdditionalCapital`）。 |
| `AdditionalCapital` | `0` | 参与风险计算的额外资金。 |
| `UseAdvancedTargets` | `false` | 使用手动设定的点数距离，而不是 ATR 倍数。 |
| `AdvancedStopPips` | `0` | 在高级模式下的止损点数。 |
| `AdvancedTakeProfitPips` | `0` | 在高级模式下的止盈点数。 |
| `UsePreviousDailyAtr` | `true` | 新交易日的前 12 小时沿用上一日的 ATR。 |
| `AtrPeriod` | `14` | ATR 周期。 |
| `AtrStopMultiplier` | `1.5` | ATR 止损倍数。 |
| `AtrTakeProfitMultiplier` | `1.0` | ATR 止盈倍数。 |
| `CandleType` | `1 Minute` | ATR 计算所用的 K 线类型。 |
| `BuyCommand` | `false` | 触发做多入场的手动开关，执行后自动复位。 |
| `SellCommand` | `false` | 触发做空入场的手动开关，执行后自动复位。 |
| `BreakevenCommand` | `false` | 将止损移动到入场价。 |
| `TrailingCommand` | `false` | 按 NNFX 规则执行一次拖尾。 |
| `CloseAllCommand` | `false` | 立即关闭所有仓位。 |

## 使用建议
- 需要证券对象提供有效的 `Step`、`StepPrice` 与 `VolumeStep` 信息，才能正确换算风险和交易量。
- 手动参数在完成的 K 线上处理，因此在切换参数后需等待下一次蜡烛更新。
- 开启手动距离时，请同时填写 `AdvancedStopPips` 和 `AdvancedTakeProfitPips`，否则策略仍会使用 ATR 倍数。
