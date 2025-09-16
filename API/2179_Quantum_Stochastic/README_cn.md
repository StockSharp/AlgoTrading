# Quantum 随机指标策略
[English](README.md) | [Русский](README_ru.md)

该策略基于随机振荡指标。当 %K 上穿 `LowLevel` 离开超卖区时开多头；当 %K 下穿 `HighLevel` 离开超买区时开空头。达到极值平仓水平后平掉仓位以锁定利润。

## 细节

- **入场条件**：
  - **多头**：%K 上穿 `LowLevel`。
  - **空头**：%K 下穿 `HighLevel`。
- **出场条件**：
  - **多头**：%K 达到 `HighCloseLevel`。
  - **空头**：%K 达到 `LowCloseLevel`。
- **指标**：Stochastic Oscillator。
- **时间框架**：参数 `CandleType`（默认 1 分钟）。
- **参数**：
  - `KPeriod` – %K 周期。
  - `DPeriod` – %D 周期。
  - `Slowing` – 平滑系数。
  - `HighLevel` – 超买区下界。
  - `LowLevel` – 超卖区上界。
  - `HighCloseLevel` – 多头平仓水平。
  - `LowCloseLevel` – 空头平仓水平。
