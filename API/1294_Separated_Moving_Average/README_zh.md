# 分离移动平均
[English](README.md) | [Русский](README_ru.md)

该策略为上涨和下跌收盘分别构建移动平均。当上涨平均线向上穿越下跌平均线时做多，反向穿越时做空。支持 SMA、EMA 或 HMA，并可使用 Heikin Ashi 价格。

## 详情

- **入场条件**: 上涨平均线上穿下跌平均线。
- **多空方向**: 双向。
- **退出条件**: 反向交叉。
- **止损**: 无。
- **默认值**:
  - `MaType` = MaType.SMA
  - `Length` = 20
  - `UseHeikinAshi` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: SMA、EMA、HMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

