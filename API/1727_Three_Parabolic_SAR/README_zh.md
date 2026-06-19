# 三重抛物线SAR策略
[English](README.md) | [Русский](README_ru.md)

三重抛物线SAR策略在6小时、3小时和1小时周期上计算三个抛物线SAR指标。当两个较高周期确认方向且1小时SAR翻转时，在1小时周期上开仓。

## 细节

- **入场条件**：
  - 长仓：6小时和3小时SAR低于收盘价，同时1小时SAR从上方跌破价格。
  - 短仓：6小时和3小时SAR高于收盘价，同时1小时SAR从下方突破价格。
- **多空方向**：双向。
- **出场条件**：当1小时SAR反向或任意高周期SAR反转时平仓。
- **止损**：无。
- **默认值**：
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `HigherTimeframe` = TimeSpan.FromHours(6)
  - `MiddleTimeframe` = TimeSpan.FromHours(3)
  - `TradingTimeframe` = TimeSpan.FromHours(1)
- **过滤器**：
  - 分类：Trend
  - 方向：Both
  - 指标：Parabolic SAR
  - 止损：无
  - 复杂度：Basic
  - 周期：Multi-timeframe
  - 季节性：No
  - 神经网络：No
  - 背离：No
  - 风险等级：Medium
