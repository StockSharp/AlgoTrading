# Bot for Spot Market - Custom Grid 策略
[English](README.md) | [Русский](README_ru.md)

Bot for Spot Market - Custom Grid 策略先建立初始头寸，当价格跌破上次买入价的指定百分比时加仓。当价格高于平均买入价达到设定的盈利百分比且持仓盈利时，策略会全部平仓。

## 详情

- **入场条件**:
  - 在开始时间买入。
  - 当价格比最近一次买入价低 `NextEntryPercent`% 时加仓。
- **方向**: 仅做多。
- **出场条件**:
  - 当价格高于平均买入价 `ProfitPercent`% 且持仓盈利时全部平仓。
- **止损**: 无。
- **默认值**:
  - `OrderValue` = 10
  - `MinAmountMovement` = 0.00001
  - `Rounding` = 5
  - `NextEntryPercent` = 0.5
  - `ProfitPercent` = 2
- **过滤器**:
  - 分类: 网格交易
  - 方向: 多头
  - 指标: 无
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
