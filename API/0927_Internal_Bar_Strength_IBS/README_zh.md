# 内部柱强度 IBS 策略
[English](README.md) | [Русский](README_ru.md)

当内部柱强度（IBS）低于下限阈值并且在指定时间窗口内时，本策略做多；当 IBS 高于上限阈值时平仓。

## 细节

- **入场条件**：
  - IBS < `LowerThreshold`
  - 时间在 `StartTime` 与 `EndTime` 之间
- **多空方向**：仅做多
- **出场条件**：
  - IBS >= `UpperThreshold`
- **止损**：无
- **默认值**：
  - `UpperThreshold` = 0.8
  - `LowerThreshold` = 0.2
- **过滤器**：
  - 分类: Mean reversion
  - 方向: Long
  - 指标: None
  - 止损: No
  - 复杂度: Low
  - 时间框架: Any
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Low
