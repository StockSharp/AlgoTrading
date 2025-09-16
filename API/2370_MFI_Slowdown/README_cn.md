# MFI减速
[English](README.md) | [Русский](README_ru.md)

该策略在较高时间框上监控资金流量指数(MFI)，当指标达到极端区域时触发信号。若启用 `SeekSlowdown`，只有当连续两根柱之间的 MFI 变化小于1 时才确认信号。出现向上信号时平掉空头并可选择开多；出现向下信号时平掉多头并可开空。风险管理通过 StartProtection 完成。

## 详情

- **入场条件**：
  - 向上：`MFI >= UpperThreshold` 且（未启用减速检查或检测到减速）。
  - 向下：`MFI <= LowerThreshold` 且（未启用减速检查或检测到减速）。
- **多/空**：根据参数可做多或做空。
- **出场条件**：
  - 反向信号平仓。
  - `StopLossPercent` 与 `TakeProfitPercent` 控制止损止盈。
- **止损**：是，使用 StartProtection。
- **默认值**：
  - `MfiPeriod` = 2
  - `UpperThreshold` = 90
  - `LowerThreshold` = 10
  - `SeekSlowdown` = true
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = 6 小时时间框
  - `BuyPosOpen` = `BuyPosClose` = `SellPosOpen` = `SellPosClose` = true
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：MFI
  - 止损：是
  - 复杂度：低
  - 时间框：任意
  - 季节性：无
  - 神经网络：无
  - 背离：可选（减速检查）
  - 风险等级：中
