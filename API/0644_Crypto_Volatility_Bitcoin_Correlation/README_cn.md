# 加密波动率与比特币相关性
[English](README.md) | [Русский](README_ru.md)

当 VIXFix 和 BVOL7D 指数同时上升且价格位于 EMA 之上时，本策略做多比特币。当价格跌破 EMA 时平仓。

## 详情

- **入场条件**：VIXFix 大于前值，BVOL7D 大于前值，收盘价高于 EMA。
- **多空方向**：仅做多。
- **出场条件**：收盘价低于 EMA。
- **止损**：无。
- **默认值**：
  - `VixFixLength` = 22
  - `EmaLength` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 类别：波动率
  - 方向：多头
  - 指标：Highest，EMA
  - 止损：无
  - 复杂度：初级
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
