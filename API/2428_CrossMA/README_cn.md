# Cross MA 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用两条简单移动平均线的交叉并结合 ATR 止损。当快速 SMA 自下而上穿越慢速 SMA 时开多头；当快速 SMA 自上而下穿越慢速 SMA 时开空头。进场后在入场价上下一个 ATR 的位置设置止损，并在每根新K线上检查是否触发。

## 参数
- K线类型
- 快速 SMA 周期
- 慢速 SMA 周期
- ATR 周期
- 交易量
