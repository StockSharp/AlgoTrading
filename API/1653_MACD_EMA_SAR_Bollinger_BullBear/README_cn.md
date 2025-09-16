# MACD EMA SAR Bollinger BullBear 策略
[English](README.md) | [Русский](README_ru.md)

结合 MACD、EMA 交叉、Parabolic SAR、布林带以及 Bulls/Bears Power 指标，仅在活跃时段进行交易。

## 细节

- **入场条件**：
  - **多头**：MACD < Signal，前两个最高价低于上轨，EMA3 > EMA34，SAR 在价格下方，Bulls Power > 0 且下降。
  - **空头**：MACD > Signal，EMA3 < EMA34，SAR 在价格上方，Bears Power < 0 且上升。
- **多空方向**：双向。
- **出场条件**：
  - 无固定退出规则；遇到相反信号时平仓。
- **止损**：无。
- **默认值**：
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15 分钟
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **筛选**：
  - 类别: Trend Following
  - 方向: 双向
  - 指标: 多个
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
