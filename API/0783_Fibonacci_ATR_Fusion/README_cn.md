# Fibonacci ATR 融合策略
[English](README.md) | [Русский](README_ru.md)

结合多个斐波那契周期的买压比率与 ATR，通过阈值交叉确定进出场，并可选用 ATR 分批止盈。

## 细节

- **入场条件**：
  - **多头**：加权平均向上穿越 `LongEntryThreshold`。
  - **空头**：加权平均向下穿越 `ShortEntryThreshold`。
- **出场条件**：
  - 加权平均穿越相反的出场阈值或持仓反转。
- **指标**：
  - 基于 ATR 的买压比率加权平均。
  - ATR 用于止盈。
- **止损**：无。
- **默认值**：
  - `LongEntryThreshold` = 58
  - `ShortEntryThreshold` = 42
  - `LongExitThreshold` = 42
  - `ShortExitThreshold` = 58
  - `Tp1Atr` = 3
  - `Tp2Atr` = 8
  - `Tp3Atr` = 14
  - `Tp1Percent` = 12
  - `Tp2Percent` = 12
  - `Tp3Percent` = 12
- **过滤器**：
  - 趋势跟随
  - 单一时间框架
  - 指标：ATR
  - 止损：无
  - 复杂度：中等
