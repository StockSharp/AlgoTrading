# Spectral RVI Crossover 策略
[English](README.md) | [Русский](README_ru.md)

Spectral RVI Crossover 策略对相对活力指数及其信号线进行平滑处理，并在平滑后的线条交叉时交易。
当平滑后的 RVI 上穿平滑后的信号线时做多，反向交叉时做空。

## 细节

- **入场条件**：平滑 RVI 上穿其平滑信号线
- **多空方向**：双向
- **出场条件**：反向交叉
- **止损**：无
- **默认参数**：
  - `RviLength` = 14
  - `SignalLength` = 4
  - `SmoothLength` = 20
- **筛选器**：
  - 类别：振荡指标
  - 方向：双向
  - 指标：RVI、SMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：4小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
