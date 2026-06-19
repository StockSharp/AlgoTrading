# MA PSAR ATR 趋势策略
[English](README.md) | [Русский](README_ru.md)

MA PSAR ATR 趋势策略结合均线交叉与日线 Parabolic SAR 过滤。只有当价格同时位于快、慢均线上方或下方且 PSAR 同向时才开仓。ATR 波动止损用于控制风险。

该方法适合希望使用动态止损的趋势跟踪者，默认在 5 分钟K线上运行。

## 详情
- **入场条件**:
  - **多头**: 快速均线 > 慢速均线，收盘价 > 快速均线，最低价 > 日线 PSAR
  - **空头**: 快速均线 < 慢速均线，收盘价 < 快速均线，最高价 < 日线 PSAR
- **多空方向**: 双向
- **退出条件**:
  - **多头**: 趋势转为空头或价格跌破 ATR 止损
  - **空头**: 趋势转为多头或价格突破 ATR 止损
- **止损**: 有，基于 ATR
- **默认值**:
  - `FastMaPeriod` = 40
  - `SlowMaPeriod` = 160
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `AtrPeriod` = 14
  - `AtrMultiplierLong` = 2m
  - `AtrMultiplierShort` = 2m
  - `UsePsarFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MA, Parabolic SAR, ATR
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
