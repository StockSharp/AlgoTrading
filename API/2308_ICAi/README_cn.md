# ICAi 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于自适应移动平均线 ICAi。指标根据标准差调整斜率并平滑价格。当指标向上转折时建立多头仓位，向下转折时建立空头仓位。

该算法可用于任何有K线数据的市场。默认设置使用4小时周期和12的平滑长度。

## 详情

- **入场条件**:
  - 多头: `Prev < PrevPrev && Current >= Prev`
  - 空头: `Prev > PrevPrev && Current <= Prev`
- **多空方向**: 双向
- **出场条件**: 相反信号
- **止损/止盈**: 可选固定止损与止盈
- **默认参数**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
- **过滤器**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: ICAi
  - 止损: 支持
  - 复杂度: 中等
  - 周期: 中期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

