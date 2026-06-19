# 布林带距离策略
[Русский](README_ru.md) | [English](README.md)

利用布林带的反转并增加额外距离过滤。价格收于上轨加距离时做空，收于下轨减距离时做多。仓位通过以价格步长表示的止盈或止损退出。

## 细节

- **入场条件**：
  - 多头：收盘价低于下轨减去距离
  - 空头：收盘价高于上轨加上距离
- **多空方向**：双向
- **出场条件**：
  - 达到止盈
  - 触及止损
- **止损**：以价格步长的绝对值
- **默认参数**：
  - `BollingerPeriod` = 4
  - `BollingerDeviation` = 2m
  - `BandDistance` = 3m
  - `ProfitTarget` = 3m
  - `LossLimit` = 20m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 分类：反转
  - 方向：双向
  - 指标：布林带
  - 止损：是
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
