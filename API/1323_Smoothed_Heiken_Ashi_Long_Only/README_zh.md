# 平滑 Heiken Ashi 仅做多策略
[English](README.md) | [Русский](README_ru.md)

该策略使用平滑的 Heikin-Ashi 蜡烛。当颜色由红变绿时买入，变回红色时平仓。

## 细节

- **入场条件**：平滑 HA 由红变绿
- **方向**：仅多头
- **出场条件**：平滑 HA 变为红色
- **止损**：无
- **默认值**：
  - `EmaLength` = 10
  - `SmoothingLength` = 10
