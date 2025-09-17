# Exp Adaptive Renko MMRec Duplex 策略

该策略将 MetaTrader 5 智能交易系统 **Exp_AdaptiveRenko_MMRec_Duplex.mq5** 迁移到 StockSharp 的高级 API。策略维护两条互不干扰的 Adaptive Renko 流：多头流跟踪新的支撑，空头流跟踪新的阻力。当多头通道出现新的支撑而空头通道失去阻力（或反过来）时，系统会开立相应方向的市价单。C# 实现保留了原始 EA 的 “MM Recounter” 资金管理模块——在连续亏损达到阈值后自动降低下单量，并在盈利恢复后恢复默认规模。

## 工作流程

1. **数据订阅**：多头与空头分别订阅各自的蜡烛类型（时间框架），并通过 `SubscribeCandles().BindEx(...)` 绑定波动率指标（ATR 或标准差），波动率决定自适应砖块的高度。
2. **Adaptive Renko 处理**：辅助类 `AdaptiveRenkoProcessor` 重建 MQL 指标逻辑，返回包含趋势、支撑和阻力的快照。所有计算均基于收盘蜡烛。
3. **入场逻辑**：当多头快照出现向上的趋势（信号蜡烛上有支撑）时，策略开多；当空头快照出现向下趋势（信号蜡烛上出现阻力）时，开空。
4. **离场逻辑**：相反的 Renko 信号触发平仓。同时按价格步长检查止损和止盈阈值。
5. **MMRec 资金管理**：每个方向维护一组最近交易盈亏的队列。若在配置窗口内的亏损次数达到 `LossTrigger`，下一笔交易使用缩减后的资金管理值（`LongSmallMoneyManagement` / `ShortSmallMoneyManagement`），否则使用常规值（`LongMoneyManagement` / `ShortMoneyManagement`）。`MarginModeOption` 枚举复现了原 EA 的所有下单模式（按手数、按余额比例、按风险比例等）。
6. **交易登记**：每次平仓都会调用 `RegisterTradeResult` 更新 MMRec 队列，队列裁剪逻辑与 MQL 中的 `BuyTradeMMRecounterS` / `SellTradeMMRecounterS` 完全一致，无需遍历历史订单。

## 参数分组

| 分组 | 关键参数 | 说明 |
| --- | --- | --- |
| 多头 | `LongCandleType`, `LongVolatilityMode`, `LongVolatilityPeriod`, `LongSensitivity`, `LongPriceMode`, `LongMinimumBrickPoints`, `LongSignalBarOffset` | 控制多头 Adaptive Renko 流的指标设置。 |
| 空头 | `ShortCandleType`, `ShortVolatilityMode`, `ShortVolatilityPeriod`, `ShortSensitivity`, `ShortPriceMode`, `ShortMinimumBrickPoints`, `ShortSignalBarOffset` | 为空头流提供与多头对称的配置。 |
| MMRec | `LongTotalTrigger`, `LongLossTrigger`, `LongSmallMoneyManagement`, `LongMoneyManagement`, `LongMarginMode`, `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, `ShortMoneyManagement`, `ShortMarginMode` | 资金管理恢复模块参数。*TotalTrigger* 定义滑动窗口长度，*LossTrigger* 决定何时切换到缩减手数。 |
| 风险控制 | `LongStopLossPoints`, `LongTakeProfitPoints`, `ShortStopLossPoints`, `ShortTakeProfitPoints`, `LongDeviationSteps`, `ShortDeviationSteps` | 以价格步长表示的止损、止盈以及信息性的滑点设置。 |

## 实现细节

- 策略按净额账户模型工作，开多前会平掉现有的空单，开空同理。
- `CalculateVolume` 将资金管理值转换为下单量，支持所有原始保证金模式，包括基于止损距离的风险控制。
- 指标计算仅在 `CandleStates.Finished` 的蜡烛上进行，严格遵循源 EA 的行为。
- 日志会记录当前使用的资金管理倍数以及预计的滑点（以价格步长表示），方便调试与复盘。

## 文件结构

- `CS/ExpAdaptiveRenkoMmrecDuplexStrategy.cs`：策略实现，包含 Adaptive Renko 处理器与 MMRec 模块。
- `README.md`：英文说明。
- `README_ru.md`：俄文说明。
- `README_cn.md`：中文说明（本文档）。
