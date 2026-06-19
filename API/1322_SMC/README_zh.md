[English](README.md) | [Русский](README_ru.md)

SMC策略利用最近的摆动高点和低点来定义溢价、均衡和折扣区域。结合SMA趋势过滤和简单订单块确认，在折扣区买入、溢价区卖出。

## 详情

- **入场条件**: 价格在折扣区且高于SMA并得到订单块支撑；价格在溢价区且低于SMA并受订单块阻力
- **多空方向**: 双向
- **出场条件**: 相反信号
- **止损**: 无
- **默认值**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
- **过滤器**:
  - 分类: Zone
  - 方向: 双向
  - 指标: Highest, Lowest, SMA
  - 止损: 无
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: Medium
