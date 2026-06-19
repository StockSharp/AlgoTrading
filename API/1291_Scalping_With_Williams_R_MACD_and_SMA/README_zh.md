# 基于 Williams R、MACD 和 SMA 的剥头皮策略
[English](README.md) | [Русский](README_ru.md)

该策略在1分钟K线中结合 Williams %R、MACD 柱状图和简单移动平均线进行剥头皮交易。

## 细节

- **入场条件**：Williams %R 穿越激活水平且 MACD 柱状图按趋势方向变号。
- **多空**：支持做多和做空。
- **退出条件**：柱状图方向反转。
- **止损**：无。
- **默认参数**：
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
