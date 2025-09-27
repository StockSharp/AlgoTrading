# RCI策略
[English](README.md) | [Русский](README_ru.md)

该策略利用秩相关指数(RCI)及其均线的交叉进行交易。当RCI上穿其均线时开多，下穿时开空。交易方向可以限制为仅做多或仅做空。

## 详情
- **入场条件**: RCI与其均线发生交叉。
- **多空方向**: 可配置（双向、仅多、仅空）。
- **退出条件**: 反向交叉。
- **止损**: 无。
- **默认值**:
  - `RciLength` = 10
  - `MaType` = SMA
  - `MaLength` = 14
  - `Direction` = Long & Short
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 可配置
  - 指标: RCI, MA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
