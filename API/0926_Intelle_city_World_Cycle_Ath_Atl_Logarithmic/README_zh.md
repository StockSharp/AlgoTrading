# Intelle city World Cycle Ath Atl Logarithmic Strategy
[English](README.md) | [Русский](README_ru.md)

该策略使用缩放移动平均线基于 Pi Cycle 概念识别历史高点 (ATH) 和历史低点 (ATL) 的信号。

当缩放后的 ATH 长期 SMA 下穿短期 SMA 时卖出；当缩放后的 ATL 长期 SMA 上穿短期 EMA 时买入。

## 细节

- **入场条件**：缩放 ATH 长期 SMA 下穿 ATH 短期 SMA 时卖出；缩放 ATL 长期 SMA 上穿 ATL 短期 EMA 时买入。
- **多空方向**：双向。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `AthLongLength` = 350
  - `AthShortLength` = 111
  - `AtlLongLength` = 471
  - `AtlShortLength` = 150
  - `CandleType` = TimeSpan.FromDays(1)
- **筛选**：
  - 类别：趋势
  - 方向：双向
  - 指标：SMA，EMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
