# ATR Sell the Rip Mean Reversion 策略
[English](README.md) | [Русский](README_ru.md)

该策略仅做空，当价格上冲至平滑的 ATR 阈值之上时卖出，在价格跌破前一个低点时平仓。可选的 EMA 过滤器仅在下行趋势中交易。

## 细节

- **入场条件**: 收盘价高于平滑的 (close + ATR * 倍数)
- **多空方向**: 做空
- **出场条件**: 收盘价低于前一个低点
- **止损**: 无
- **默认值**:
  - `AtrPeriod` = 20
  - `AtrMultiplier` = 1.0
  - `SmoothPeriod` = 10
  - `EmaPeriod` = 200
- **过滤器**:
  - 分类: Mean Reversion
  - 方向: 做空
  - 指标: ATR, SMA, EMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
