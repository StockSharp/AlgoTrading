# RSI Long Only with Confirmed Crossbacks 策略
[English](README.md) | [Русский](README_ru.md)

该策略等待 RSI 先跌破某个阈值，再从下方回到该阈值上方。这个确认的回升视为超卖反弹，随后开多仓。RSI 上穿退出水平时平仓。参数允许做空，但默认值实际禁用做空。

## 详情

- **入场条件**：RSI 先低于超卖水平，随后向上穿越该水平。
- **方向**：默认仅做多。
- **出场条件**：RSI 上穿多头退出水平或触发做空规则。
- **止损**：否。
- **默认值**：
  - `CandleType` = 5 分钟
  - `RsiLength` = 14
  - `Oversold` = 44
  - `LongExitLevel` = 70
  - `ShortEntryLevel` = 100
  - `ShortExitLevel` = 0
- **筛选器**：
  - 类别：反转
  - 方向：多头
  - 指标：RSI
  - 止损：否
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
