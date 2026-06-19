# 资金止损止盈策略
[English](README.md) | [Русский](README_ru.md)

当短期SMA上穿长期SMA时做多，下穿时做空。当盈利或亏损达到设定的资金数额时平仓。

## 详情

- **入场条件**：SMA(14) 上穿或下穿 SMA(28)
- **方向**：双向
- **出场条件**：盈利或亏损达到设定金额
- **止损**：是
- **默认值**：
  - `FastLength` = 14
  - `SlowLength` = 28
  - `TakeProfitMoney` = 200
  - `StopLossMoney` = 100
