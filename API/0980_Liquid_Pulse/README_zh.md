# Liquid Pulse Strategy
[English](README.md) | [Русский](README_ru.md)

该策略检测由 MACD 和 ADX 确认的高成交量激增。ATR 定义止损和止盈，并限制每日交易次数。

## 细节

- **入场条件**:
  - 多头：成交量激增，MACD 上穿信号线，+DI > -DI，ADX ≥ 阈值
  - 空头：成交量激增，MACD 下穿信号线，-DI > +DI，ADX ≥ 阈值
- **方向**：双向
- **出场条件**：基于 ATR 的止损或止盈
- **止损**：ATR 倍数
- **默认值**：
  - `VolumeSensitivity` = Medium
  - `MacdSpeed` = Medium
  - `DailyTradeLimit` = 20
  - `AtrPeriod` = 9
  - `AdxTrendThreshold` = 41
- **筛选**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：MACD、ADX、ATR、成交量
  - 止损：是
  - 复杂度：中等
  - 周期：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
