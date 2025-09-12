# How To Set Backtest Time Ranges
[English](README.md) | [Русский](README_ru.md)

该策略演示如何将交易限制在特定的日期和日内时间范围内。快速SMA上穿慢速SMA时开多，反向下穿时平仓。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: 在选定的日期和入场时间范围内，快速SMA上穿慢速SMA。
- **离场条件**: 在选定的日期和出场时间范围内，快速SMA下穿慢速SMA。
- **止损**: 无。
- **默认参数**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `FromDate` = 2021-01-01
  - `ThruDate` = 2112-01-01
  - `EntryStart` = 00:00
  - `EntryEnd` = 00:00
  - `ExitStart` = 00:00
  - `ExitEnd` = 00:00
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 多头
  - 指标: SMA
  - 复杂度: 低
  - 风险级别: 中等
