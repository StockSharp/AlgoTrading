# MFI 退出超卖区并加仓策略
[English](README.md) | [Русский](README_ru.md)

该策略等待资金流量指数（MFI）进入超卖区。当 MFI 回到超卖水平之上时，在当前收盘价下方按固定百分比设置限价买单。如果在设定的 bar 数内未成交，则取消订单。通过 StartProtection 设置止损和止盈。

## 详情

- **入场条件**：
  - MFI 从低于 `MfiOversoldLevel` 上穿该水平后，在收盘价下 `LongEntryPercentage`% 放置限价买单。
- **多/空**：仅多头。
- **出场条件**：
  - 通过止盈或止损 (`ExitGainPercentage`, `StopLossPercentage`) 平仓。
- **止损**：是，使用 StartProtection。
- **默认值**：
  - `MfiPeriod` = 14
  - `MfiOversoldLevel` = 20
  - `LongEntryPercentage` = 0.1
  - `StopLossPercentage` = 1
  - `ExitGainPercentage` = 1
  - `CancelAfterBars` = 5
- **过滤条件**：
  - 分类：均值回归
  - 方向：多头
  - 指标：MFI
  - 止损：是
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低
