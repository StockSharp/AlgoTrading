# Tick Marubozu 策略
[English](README.md) | [Русский](README_ru.md)

在tick数据上识别Marubozu形态并结合放量确认。出现多头Marubozu时买入，空头Marubozu时卖出。

## 细节

- **入场条件**: 高于SMA的多头或空头Marubozu
- **多/空**: 双向
- **离场条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `TickSize` = 5
  - `VolLength` = 20
  - `CandleType` = 1分钟周期
- **过滤器**:
  - 分类: 形态
  - 方向: 双向
  - 指标: SMA
  - 止损: 无
  - 复杂度: 基础
  - 周期: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
