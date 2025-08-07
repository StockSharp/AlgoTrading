# DMI Winner
[English](README.md) | [Русский](README_ru.md)

DMI Winner 是基于方向性运动指数 (DMI) 的趋势策略。
当 `+DI` 与 `-DI` 交叉且 ADX 高于关键水平时，表示趋势增强，策略据此开仓。

可选的移动平均过滤器帮助顺应更大级别的趋势。默认无止损，
但可通过参数启用。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: `+DI` 上穿 `-DI` 且 `ADX` > `KeyLevel`（可选 MA 过滤）。
  - **空头**: `-DI` 上穿 `+DI` 且 `ADX` > `KeyLevel`（可选 MA 过滤）。
- **离场条件**: DI 反向交叉或启用时触发止损。
- **止损**: 可选止损 (`UseSL`)。
- **默认参数**:
  - `DILength` = 14
  - `KeyLevel` = 23
  - `UseMA` = True
  - `UseSL` = False
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 多空皆可
  - 指标: DMI, Moving Average
  - 复杂度: 中等
  - 风险级别: 中等
