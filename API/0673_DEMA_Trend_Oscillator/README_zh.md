# DEMA 趋势振荡策略
[English](README.md) | [Русский](README_ru.md)

该策略通过移动平均和标准差对双指数移动平均线（DEMA）进行归一化。当归一化值高于多头阈值且价格位于上轨之上时做多；当归一化值低于空头阈值且价格位于下轨之下时做空。使用基于 ATR 的追踪止损、带状止损以及按风险回报比计算的止盈。

## 细节

- **入场条件**：
  - 多头：归一化值 > `LongThreshold` 且最低价 > 上轨
  - 空头：归一化值 < `ShortThreshold` 且最高价 < 下轨
- **方向**：双向
- **出场条件**：
  - 多头：触及止盈、带状止损或追踪止损
  - 空头：触及止盈、带状止损或追踪止损
- **止损**：带状止损、ATR 追踪止损、风险回报止盈
- **默认值**：
  - `DemaPeriod` = 40
  - `BaseLength` = 20
  - `LongThreshold` = 55m
  - `ShortThreshold` = 45m
  - `RiskReward` = 1.5m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：DEMA、SMA、StandardDeviation、ATR
  - 止损：有
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
