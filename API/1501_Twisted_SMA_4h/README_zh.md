# Twisted SMA 策略 4h
[English](README.md) | [Русский](README_ru.md)

Twisted SMA 策略在 4 小时级别使用三条简单移动平均线和 KAMA 过滤器。当快速 SMA 高于中速 SMA、中速高于慢速，且收盘价高于主 SMA 并且 KAMA 不处于平坦状态时开多。当三条 SMA 呈现看跌排列时平仓。

## 细节

- **入场条件**：快速 SMA > 中速 SMA > 慢速 SMA，收盘价 > 主 SMA，且 KAMA 不平坦。
- **多空方向**：仅做多。
- **出场条件**：快速 SMA < 中速 SMA < 慢速 SMA。
- **止损**：无。
- **默认值**：
  - `FastLength` = 4
  - `MidLength` = 9
  - `SlowLength` = 18
  - `MainSmaLength` = 100
  - `KamaLength` = 25
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别：Trend
  - 方向：Long
  - 指标：SMA, KAMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
