# XAUUSD 简单的20美元止盈/100美元止损策略
[English](README.md) | [Русский](README_ru.md)

当没有持仓且两个冷却计时器都结束时，策略建立多头仓位。
当浮动盈亏达到20美元或亏损达到100美元时，策略平仓。
盈利平仓后等待15分钟才能再次入场，亏损平仓后等待12小时。

## 参数

- `ProfitTarget` – 以美元计的止盈。
- `LossLimit` – 以美元计的止损。
- `TradeCooldown` – 亏损后的等待时间。
- `EntryCooldown` – 盈利后的等待时间。
- `CandleType` – K线周期。
