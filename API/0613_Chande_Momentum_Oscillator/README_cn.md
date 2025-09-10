# Chande 动量振荡器策略
[English](README.md) | [Русский](README_ru.md)

当 Chande 动量振荡器跌破下阈值时买入，当其升破上阈值或持仓达到固定的柱数时平仓。

测试表明年化收益率约为 40%。该策略在趋势性市场表现最佳。

该振荡器比较近期的上涨和下跌幅度以评估动量。极度负值暗示市场超卖，策略据此建立多头头寸。当动量转为正或持仓时间结束时退出。

## 细节

- **入场条件**：`CMO < LowerThreshold`。
- **多空方向**：仅做多。
- **出场条件**：`CMO > UpperThreshold` 或超过 `MaxBarsInPosition` 根柱。
- **止损**：无。
- **默认值**：
  - `CmoPeriod` = 9
  - `LowerThreshold` = -50
  - `UpperThreshold` = 50
  - `MaxBarsInPosition` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 分类：均值回归
  - 方向：多头
  - 指标：CMO
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
