# Delta SMA 1-Year High Low
[English](README.md) | [Русский](README_ru.md)

**Delta SMA 1-Year High Low** 策略计算买卖量差值及其简单移动平均。当 delta SMA 处于极低水平后向上穿越零线时做多；当 delta SMA 先上穿年内高值的70%后又跌破该高值的60%时平仓。

## 细节
- **入场条件**：delta SMA 低于一年最低值的70%并向上穿越零线。
- **多空方向**：仅做多。
- **出场条件**：delta SMA 在先上穿一年最高值的70%后跌破该最高值的60%。
- **止损**：无。
- **默认值**：
  - `DeltaSmaLength = 14`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **过滤器**：
  - 分类：Volume
  - 方向：多头
  - 指标：SMA, Highest, Lowest
  - 止损：无
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
