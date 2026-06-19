# Bollinger Winner Pro
[English](README.md) | [Русский](README_ru.md)

Bollinger Winner Pro 在 Lite 基础上增加了可选过滤器和风险控制。
当价格收于布林带之外时，只有所有启用的过滤器都确认后才会开仓。

可启用的过滤器包括 RSI、Aroon 和移动平均线，并可选择使用止损。
这种模块化设计使策略能适应不同市场。

策略以均值回归为目标，当价格回到带内或触及相反带时平仓，
若启用止损则按 `UseSL` 参数执行。

## 细节
- **数据**: 价格K线。
- **入场条件**: 收盘价位于带外且所有启用过滤器同意。
- **离场条件**: 回到中轨或相反带，或在启用时触发止损。
- **止损**: 可选止损（`UseSL`）。
- **默认参数**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **过滤器**:
  - 类型: 带有确认的均值回归
  - 方向: 多空皆可
  - 指标: Bollinger Bands, RSI, Aroon, Moving Average
  - 复杂度: 高
  - 风险级别: 中等
