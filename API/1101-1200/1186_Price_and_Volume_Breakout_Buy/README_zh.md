# 价格与成交量突破买入策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

当价格与成交量同时突破各自周期高点且价格位于趋势SMA之上时，策略开多。若价格跌破周期低点并满足相同的成交量条件且价格位于SMA之下，则开空。若价格连续五根K线收于SMA另一侧，则平仓。

## 细节
- **入场条件**：
  - **多头**：收盘价 > 前周期最高价 && 成交量 > 前周期最大成交量 && 收盘价 > SMA
  - **空头**：收盘价 < 前周期最低价 && 成交量 > 前周期最大成交量 && 收盘价 < SMA
- **多空方向**：可配置
- **出场条件**：
  - **趋势**：连续五根K线收于SMA另一侧
- **止损**：无
- **默认值**：
  - `PriceBreakoutPeriod` = 60
  - `VolumeBreakoutPeriod` = 60
  - `TrendlineLength` = 200
  - `OrderDirection` = "Long"
  - `CandleType` = TimeSpan.FromDays(1)
- **筛选器**：
  - 分类：突破
  - 方向：可配置
  - 指标：Highest、SMA、Volume
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

价格和成交量极值仅使用此前已完成的K线（不包含当前信号K线）。`OrderDirection` 可设为 `Long`、`Short` 或 `Both`，默认 `Long`。收盘价回到持仓一侧或恰好位于SMA时，连续五根K线的离场计数重新开始。
