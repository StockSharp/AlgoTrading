# Alligator Simple 策略

## 概述
Alligator Simple 策略在 StockSharp 高级 API 中重现了 MetaTrader 的“Alligator Simple v1.0”专家顾问。策略只在已完成的 K 线中读取比尔·威廉姆斯的 Alligator 指标，并在上一根完成蜡烛的 Lips、Teeth、Jaw 三条线按照同一方向张开时建立仓位。每笔交易都可以按点值设置止损、止盈和移动止损，与原始 MQL 实现保持一致。

## 指标与数据
- **Alligator 线**：对蜡烛的中值价格 `(最高价 + 最低价) / 2` 计算的三条平滑移动平均线（SMMA），并为 Jaw、Teeth、Lips 提供独立的周期与前移位数。
- **蜡烛数据**：策略订阅一个可配置的 `CandleType`（默认 1 小时）并仅处理收盘完成的蜡烛，避免提前看到未来数据。

## 交易逻辑
1. **信号判断**
   - 读取上一根完成蜡烛对应的移位后 Alligator 数值。
   - 多头条件：`Lips[t-1] > Teeth[t-1] > Jaw[t-1]`。
   - 空头条件：`Lips[t-1] < Teeth[t-1] < Jaw[t-1]`。
2. **执行规则**
   - 当没有持仓时，按照 `OrderVolume` 市价开仓。
   - 同一时间仅持有一个方向的仓位，直到当前仓位平仓前忽略反向信号。

## 出场与风险控制
- **初始止损**：若 `StopLossPips > 0`，则按符号的最小报价步长（对 3/5 位小数品种会乘以 10）换算点值，将止损设置在成交价的相应距离。
- **止盈**：当 `TakeProfitPips > 0` 时，在成交价附近对称设置止盈，值为零时禁用。
- **移动止损**：当 `TrailingStopPips` 与 `TrailingStepPips` 都为正数时，价格向有利方向至少移动 `TrailingStop + TrailingStep` 后，将止损上移/下移到 `收盘价 − TrailingStop`（多头）或 `收盘价 + TrailingStop`（空头）。移动止损根据蜡烛的高低价模拟盘中触发。
- **离场处理**：止损、止盈和移动止损在每根完成的蜡烛上检查，并通过市价单平仓。

## 参数
- `OrderVolume`（默认 **1**）：下单数量（手数或合约数）。
- `StopLossPips`（默认 **100**）：初始止损点数，设为零则禁用。
- `TakeProfitPips`（默认 **100**）：止盈点数，设为零则禁用。
- `TrailingStopPips`（默认 **5**）：移动止损点数，设为零禁用移动止损。
- `TrailingStepPips`（默认 **5**）：在移动止损调整前所需的额外点数，启用移动止损时必须为正。
- `JawPeriod`、`TeethPeriod`、`LipsPeriod`：Jaw、Teeth、Lips 的 SMMA 周期（默认 13/8/5）。
- `JawShift`、`TeethShift`、`LipsShift`：读取 Alligator 数值时使用的前移位数（默认 8/5/3）。
- `CandleType`：用于计算的蜡烛类型/周期（默认 1 小时）。

## 实现说明
- 点值根据交易品种的最小价格步长自动调整；对于三位或五位小数的品种会乘以 10，以复现 MetaTrader 中的点值定义。
- 指标历史缓冲区保存足够的数值以支持配置的前移位数，无需手动数组操作。
- 策略使用 `BuyMarket` 与 `SellMarket` 高级方法提交订单，从而将代码重点放在信号与风控逻辑上。
