# Tick Data Detailed 策略
[English](README.md) | [Русский](README_ru.md)

收集并按预设区间统计买卖方向的逐笔成交量，用于详细的盘口分析，不生成交易信号。

## 细节

- **入场条件**: 无
- **多/空**: 无
- **离场条件**: 无
- **止损**: 无
- **默认值**:
  - `VolumeLessThan` = 10000
  - `Volume2From` = 10000
  - `Volume2To` = 20000
  - `Volume3From` = 20000
  - `Volume3To` = 50000
  - `Volume4From` = 50000
  - `Volume4To` = 100000
  - `Volume5From` = 100000
  - `Volume5To` = 200000
  - `Volume6From` = 200000
  - `Volume6To` = 400000
  - `VolumeGreaterThan` = 400000
- **过滤器**:
  - 分类: 成交量
  - 方向: 无
  - 指标: 无
  - 止损: 无
  - 复杂度: 基础
  - 周期: Tick
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 低
