# Color Zerolag HLR 策略

该策略是 MQL5 Expert `Exp_ColorZerolagHLR` 的 C# 版本。策略使用五个不同周期的 Hi-Lo Range (HLR) 振荡器，通过加权求和形成快速线，然后应用零滞后平滑得到慢速线。两条线的交叉用作交易信号。

## 概述
- 根据指定周期计算五个 HLR 值。
- 加权求和得到快速趋势线。
- 使用指数平滑得到慢速趋势线。
- 快速线与慢速线交叉时进行交易。

## 参数
- `Smoothing` – 平滑系数。
- `Factor1`..`Factor5` – 各 HLR 的权重。
- `HlrPeriod1`..`HlrPeriod5` – HLR 的周期。
- `BuyPosOpen`/`SellPosOpen` – 是否允许开多/开空。
- `BuyPosClose`/`SellPosClose` – 是否允许平多/平空。
- `CandleType` – K 线周期。

## 指标
- Highest、Lowest（各五个）。

## 交易逻辑
- 如果上一根柱快速线在慢速线之上，而当前交叉到下方，则开多并平空。
- 如果上一根柱快速线在慢速线之下，而当前交叉到上方，则开空并平多。

此策略示例用于学习，可根据需求调整参数和风控设置。
