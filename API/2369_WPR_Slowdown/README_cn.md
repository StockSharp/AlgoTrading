# WPR Slowdown 策略
[English](README.md) | [Русский](README_ru.md)

WPR Slowdown 策略利用 Williams %R 振荡器在极值附近动量减弱时寻找反转。当当前 Williams %R 与前一个值的差异小于 1 时认为出现“减速”。在上限以上出现减速时，策略会关闭空头仓位并在允许的情况下开多；在下限以下出现减速时，策略会关闭多头仓位并在允许的情况下开空。

## 入场与出场规则
- **开多**：Williams %R 高于 `LevelMax` 且满足减速条件，可选关闭空头。
- **开空**：Williams %R 低于 `LevelMin` 且满足减速条件，可选关闭多头。
- **平多**：在启用 `BuyPosClose` 时出现卖出信号。
- **平空**：在启用 `SellPosClose` 时出现买入信号。

## 参数
- `WprPeriod` – Williams %R 的计算周期。
- `LevelMax` – 上方信号阈值（默认 -20），表示超买区域。
- `LevelMin` – 下方信号阈值（默认 -80），表示超卖区域。
- `SeekSlowdown` – 是否检查 Williams %R 相邻值之间的减速。
- `BuyPosOpen` – 允许开多。
- `SellPosOpen` – 允许开空。
- `BuyPosClose` – 允许在卖出信号时平多。
- `SellPosClose` – 允许在买入信号时平空。
- `CandleType` – 用于计算的 K 线类型（默认 6 小时 K 线）。

## 说明
该策略仅保留原始 MQL5 专家的 Williams %R 减速逻辑。提醒、资金管理等附加功能已被省略，若需要可手动添加止损与止盈。
