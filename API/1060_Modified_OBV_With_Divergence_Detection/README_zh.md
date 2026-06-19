# 具有背离检测的改进 OBV
[English](README.md) | [Русский](README_ru.md)

该策略使用可选类型的移动平均线对 OBV 进行平滑，并生成信号线。当平滑后的 OBV 与信号线交叉时开仓或反向。策略还使用分形检测记录价格与 OBV 之间的常规和隐藏背离。

## 细节

- **入场条件**：OBV-M 与信号线交叉。
- **多空方向**：双向。
- **出场条件**：反向交叉。
- **止损**：否。
- **默认值**：
  - `MaType` = Exponential
  - `ObvMaLength` = 7
  - `SignalLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：背离
  - 方向：双向
  - 指标：OBV, MA
  - 止损：否
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：是
  - 风险等级：中等
