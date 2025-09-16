# 移动平均与帧
[English](README.md) | [Русский](README_ru.md)

移植自 MetaTrader 5 专家顾问 **“Moving Average with Frames”**。原版通过比较每根 K 线的开盘价和收盘价与向前平移的简单移动平均线（SMA）来生成信号，并在优化后绘制多条权益曲线。本移植版聚焦交易逻辑：仅在每根完成的 K 线后响应，在净额制度下维持单一仓位，并完整保留原策略的资金管理规则。

## 交易逻辑

- **数据来源**：订阅参数 `CandleType` 指定的时间框架，只处理已经收盘的蜡烛，复刻 MQL5 中的 `if(rt[1].tick_volume>1) return;` 限制。
- **指标**：使用周期为 `MovingPeriod` 的简单移动平均线。通过内部缓冲区将指标值向前平移 `MovingShift` 根已完成的蜡烛。
- **预热阶段**：在累积到至少 100 根完成的蜡烛之前禁止交易，对应原程序的 `Bars(_Symbol,_Period)>100` 检查。
- **入场条件**
  - **做多**：蜡烛开盘价低于平移后的 SMA，收盘价高于 SMA。
  - **做空**：蜡烛开盘价高于平移后的 SMA，收盘价低于 SMA。
  - 策略始终只持有一个方向的净仓，信号反转时先平掉旧仓再开新仓。
- **离场条件**：多头在价格重新跌破平移 SMA 时平仓，空头在价格重新上穿平移 SMA 时平仓。与原策略一致，同一根 K 线不会在平仓后立刻重新开仓。

## 风险与仓位控制

- **MaximumRisk**：当投资组合市值可用时，按照 `Portfolio.CurrentValue * MaximumRisk / price` 估算下单量；否则回退到手动设置的 `Volume`。
- **DecreaseFactor**：连续超过一次亏损后，将下一笔下单量减少 `volume * losses / DecreaseFactor`，完全复刻 MetaTrader 的“递减手数”逻辑；盈利交易会清零计数。
- **数量规范化**：计算出的下单量会按照合约的 `VolumeStep` 对齐，并限制在 `MinVolume` 与 `MaxVolume` 之间；若没有成交量步长信息，则四舍五入保留两位小数。

## 其他说明

- MetaTrader 的“帧”可视化未迁移，StockSharp 已提供完善的优化分析工具；本移植保持了信号与资金管理的一致性。
- 指标数值通过 `Bind` 回调直接获取，没有调用 `GetValue`。
- 连续亏损统计放在 `OnOwnTradeReceived` 中，实现对部分成交和净额模式的正确处理。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `MaximumRisk` | `0.02` | 每次建仓所风险的账户权益比例。 |
| `DecreaseFactor` | `3` | 当出现连续亏损时用于缩减手数的除数。 |
| `MovingPeriod` | `12` | 计算简单移动平均的周期（使用收盘价）。 |
| `MovingShift` | `6` | 将 SMA 向前平移的已完成蜡烛数量。 |
| `CandleType` | `1 小时时间框架` | 策略订阅并处理的主蜡烛序列。 |

## 使用建议

1. 在 StockSharp Designer 或代码中将策略绑定到目标证券与投资组合。
2. 根据原 MT5 图表调整 `CandleType`，确保时间框架一致。
3. 按账户规模与风险承受能力微调 `MaximumRisk` 与 `DecreaseFactor`。
4. 通过回测验证交叉信号与原专家顾问的表现是否一致。
