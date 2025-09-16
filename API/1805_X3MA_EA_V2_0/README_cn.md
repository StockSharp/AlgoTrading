# 三重均线交叉策略

该策略基于三条移动平均线（快线、中线、慢线）之间的关系进行交易，源自 MQL EA **X3MA_EA_V2_0**。

## 交易逻辑

* **入场**
  * 当 *EnableEntryMediumSlowCross* 为真时，中线向上穿越慢线时做多，反向穿越时做空。
  * 当该选项为假时，策略等待快线穿越中线且两者位于慢线同一侧。多头条件为 `fast > medium > slow`，空头条件为 `fast < medium < slow`。
* **出场**
  * 当 *EnableExitFastSlowCross* 为真时，快线与慢线发生反向交叉时平仓。

所有信号都在已完成的K线基础上计算。

## 参数

| 名称 | 描述 |
|------|------|
| `FastMaLength` | 快速均线周期。 |
| `MediumMaLength` | 中速均线周期。 |
| `SlowMaLength` | 慢速均线周期。 |
| `EnableEntryMediumSlowCross` | 允许在中线与慢线交叉时入场。 |
| `EnableExitFastSlowCross` | 快线与慢线交叉时平仓。 |
| `CandleType` | K线时间框架。 |

## 备注

策略使用 `SubscribeCandles` 与 `Bind` 的高级 API 实现，指标值通过 `ProcessCandle` 回调获取，不使用 `GetValue`。在 `OnStarted` 中调用 `StartProtection()` 启用仓位保护。
