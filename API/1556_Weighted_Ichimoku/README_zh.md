# Weighted Ichimoku 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 Ichimoku 指标信号组合成加权得分。
当得分超过买入阈值时买入，得分跌破卖出阈值时退出。

## 细节

- **入场条件**: 得分 >= BuyThreshold
- **多空方向**: 仅多头
- **出场条件**: 得分 <= SellThreshold 或低于零（如果关闭阈值）
- **止损**: 无
- **默认值**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `Offset` = 26
  - `BuyThreshold` = 60
  - `SellThreshold` = -49
- **过滤器**:
  - 分类: 趋势
  - 方向: 多头
  - 指标: Ichimoku
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

