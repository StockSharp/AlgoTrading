# KumoTrade Ichimoku 策略
[English](README.md) | [Русский](README_ru.md)

基于 Ichimoku 云图和随机震荡指标的策略。
当价格回到 Kijun 上方、随机指标超卖且前方无云层时做多。
当价格跌破云层、随机指标超买且云层呈看跌时做空。

## 细节

- **入场条件**：
  - 多头：`Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - 空头：`Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **方向**：多空皆可
- **出场条件**：
  - 基于 ATR 的移动止损
- **止损**：ATR * 3 的追踪止损
- **默认值**：
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：Ichimoku 云图，Stochastic，ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
