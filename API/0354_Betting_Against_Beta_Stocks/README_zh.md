# Betting Against Beta Stocks
[English](README.md) | [Русский](README_ru.md)

**Betting Against Beta Stocks** 策略在股票池中做多最低贝塔十分位并做空最高贝塔十分位，于每月的第一个交易日进行再平衡。

该方法利用低贝塔股票在风险调整后表现更优的现象，计算贝塔时需要一个基准证券。

## 详情
- **入场条件**：每月选择低/高贝塔股票。
- **多空方向**：双向。
- **退出条件**：在下一次再平衡时调整仓位。
- **止损**：无明确止损逻辑。
- **默认值**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
  - `MinTradeUsd = 100`
- **过滤器**:
  - 分类: 统计
  - 方向: 双向
  - 指标: 贝塔
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
