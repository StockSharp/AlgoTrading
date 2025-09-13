# YURAZ CLOSEPRC V3
[Русский](README_ru.md) | [English](README.md)

该风险管理辅助策略在投资组合利润超过初始资金的设定百分比时关闭当前仓位。它仿照 MetaTrader 脚本 `YURAZ_CLOSEPRC_V3_1.mq5` 的功能，在达到利润目标后允许一键平仓。策略在每根完成的K线后检查权益，当达到阈值时发送市价单退出。

## 详情

- **用途**：在达到利润目标时平仓
- **交易**：示例
- **指标**：无
- **止损**：无
- **默认值**：
  - `ProfitPercent` = 10
  - `CandleType` = 1 minute

## 备注

- 利润按 `Portfolio.CurrentValue` 相对于启动时数值的百分比变化计算。
- 满足条件后，策略发送市价单关闭全部仓位。
