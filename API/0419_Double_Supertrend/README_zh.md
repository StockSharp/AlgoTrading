# Double Supertrend
[English](README.md) | [Русский](README_ru.md)

Double Supertrend 使用两个基于 ATR 的移动平均线，周期和倍数各不相同。
第一条线确定交易方向，第二条线可作为目标或跟踪退出，从而在趋势交易中
提供灵活的盈亏控制。

当价格位于两条线之上且允许做多时开多单；做空条件类似。退出依据所选
的止盈类型或百分比止损。

## 细节
- **数据**: 价格K线。
- **入场条件**: 价格按允许的 `Direction` 穿越 Supertrend 线。
- **离场条件**: 破坏相反线、触发止盈 (`TPType`/`TPPercent`) 或止损 (`SLPercent`)。
- **止损**: 按价格百分比的止损 (`SLPercent`)。
- **默认参数**:
  - `ATRPeriod1` = 10
  - `Factor1` = 3.0
  - `ATRPeriod2` = 20
  - `Factor2` = 5.0
  - `Direction` = "Long"
  - `TPType` = "Supertrend"
  - `TPPercent` = 1.5
  - `SLPercent` = 10.0
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 可配置
  - 指标: 基于 ATR 的 Supertrend
  - 复杂度: 高
  - 风险级别: 中等
