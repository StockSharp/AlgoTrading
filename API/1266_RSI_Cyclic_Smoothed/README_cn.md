# RSI Cyclic Smoothed
[English](README.md) | [Русский](README_ru.md)

该策略基于周期平滑 RSI 指标。计算动态百分位带，当振荡器穿越这些带时进行反向交易。

## 详情

- **入场条件**：CRSI 上穿下轨或下穿上轨。
- **多/空**：双向。
- **出场条件**：穿越相反的轨道。
- **止损**：有。
- **默认值**：
  - `DominantCycleLength` = 20
  - `Vibration` = 10
  - `Leveling` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选**：
  - 分类：振荡器
  - 方向：双向
  - 指标：RSI
  - 止损：有
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：是
  - 风险级别：中
