# Keltner Channel Golden Cross 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格突破肯特纳通道并由两条 EMA 的黄金交叉确认时进场。

- **做多**：价格突破上轨且短期 EMA 高于长期 EMA。
- **做空**：价格跌破下轨且短期 EMA 低于长期 EMA。
- **退出**：基于 ATR 的止盈或止损。
- **指标**：移动平均线 (SMA/EMA/WMA)、平均真实波动幅度。
- **方向**：双向。
