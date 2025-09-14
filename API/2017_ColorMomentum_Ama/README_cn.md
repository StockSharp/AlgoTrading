# Color Momentum AMA 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MetaTrader 专家顾问 *Exp_ColorMomentum_AMA* 转换为 StockSharp。
它计算指定周期的价格动量，并使用 Kaufman 自适应移动平均线 (AMA) 进行平滑。
当平滑后的动量连续两根柱线向上或向下时产生交易信号。

## 逻辑
- **做多**：动量 AMA 连续两根柱线上升。开多之前会先平掉已有的空头。
- **做空**：动量 AMA 连续两根柱线下降。开空之前会先平掉已有的多头。
- 反向信号关闭当前仓位。

## 参数
- K线类型
- 动量周期
- AMA 周期
- 快速周期
- 慢速周期
- 信号柱
