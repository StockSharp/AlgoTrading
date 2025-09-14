# ROC2 VG 策略
[Русский](README_ru.md) | [English](README.md)

在 StockSharp 中重现 MetaTrader 的 **Exp_ROC2_VG** 专家。  
比较两条可配置周期和计算方式的价格变化率线。  
当第一条线从上向下穿越第二条线时开多，  
当第一条线从下向上穿越第二条线时开空。`Invert` 参数可以交换两条线。

## 详情

- **多头入场**：前一个 up > 前一个 down 且当前 up <= 当前 down。
- **空头入场**：前一个 up < 前一个 down 且当前 up >= 当前 down。
- **退出**：反向信号立即通过市价单反转仓位。
- **时间框架**：可配置的 K 线类型，默认 4 小时。
- **指标**：每条线可以使用 Momentum 或 ROC 变体：
  - Momentum = `价格 - 前一价格`
  - ROC = `((价格 / 前一价格) - 1) * 100`
  - ROCP = `(价格 - 前一价格) / 前一价格`
  - ROCR = `价格 / 前一价格`
  - ROCR100 = `(价格 / 前一价格) * 100`
- **默认参数**：
  - `RocPeriod1` = 8, `RocType1` = Momentum
  - `RocPeriod2` = 14, `RocType2` = Momentum
  - `Invert` = false

策略在信号变化时立即反转仓位。
