# Collector v1.0 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格触及按固定距离划分的动态买入或卖出水平时开立市价单。在指定的交易次数后，交易量会增加。当累计利润超过阈值时，所有仓位将被关闭。

## 细节

- **入场条件**:
  - 多头：收盘价 >= 买入水平
  - 空头：收盘价 <= 卖出水平
- **方向**: 双向
- **出场条件**:
  - 当总利润 >= ProfitClose 时全部平仓
- **止损**: 无
- **默认值**:
  - `Distance` = 10m
  - `InitialVolume` = 0.01m
  - `VolumeStep` = 0.01m
  - `IncreaseTrade` = 3
  - `MaxTrades` = 200
  - `ProfitClose` = 500000m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**:
  - 类别: 网格
  - 方向: 双向
  - 指标: 无
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 高
