# Ichimoku RSI MACD 策略
[English](README.md) | [Русский](README_ru.md)

结合 Ichimoku 云、RSI 和 MACD 交叉信号的趋势跟随策略。

## 细节

- **入场条件**：价格位于 Ichimoku 云之上/之下，配合 RSI 过滤和 MACD 线穿越信号线。
- **多空方向**：双向。
- **出场条件**：相反的 MACD 交叉。
- **止损**：无。
- **默认值**：
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 类别：Trend Following
  - 方向：双向
  - 指标：Ichimoku、RSI、MACD
  - 止损：无
  - 复杂度：初级
  - 时间框架：日内 (1 小时)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
