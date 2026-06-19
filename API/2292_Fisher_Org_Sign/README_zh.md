# Fisher Org 信号策略
[English](README.md) | [Русский](README_ru.md)

该策略使用带有预设上下阈值的 Fisher 变换指标。当 Fisher 值上穿下限时开多仓；当 Fisher 值下穿上限时开空仓。

## 细节

- **入场条件**：
  - **做多**：`Fisher 上穿 DownLevel`
  - **做空**：`Fisher 下穿 UpLevel`
- **多空方向**：双向
- **出场条件**：
  - 反向信号触发仓位反转
- **止损**：无
- **默认值**：
  - `Fisher Length` = 7
  - `UpLevel` = 1.5
  - `DownLevel` = -1.5
- **过滤器**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：Fisher Transform
  - 止损：无
  - 复杂度：低
  - 时间框架：中期 (H4)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中
