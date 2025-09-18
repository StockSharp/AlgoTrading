# 5Mins Envelopes
[English](README.md) | [Русский](README_ru.md)

**5Mins Envelopes** 策略复刻自 MetaTrader 专家顾问，围绕线性加权移动平均线 (LWMA) 构建包络，在 5 分钟周期上捕捉价格的极端偏离并逆向进场。
策略检测价格是否远离包络带并期待回归均值，同时保留原策略中的点差过滤、固定止损、可选止盈以及移动止损管理。

## 交易逻辑
- **指标**：以中值价格 (最高价+最低价)/2 计算的线性加权移动平均线，周期为 3。
- **包络宽度**：LWMA 的上下偏移 0.05%。
- **信号判定**（基于上一根已完成 K 线与当前买价）：
  - **做多**：上一根 K 线最低价低于下轨超过 `DistancePoints`，且当前买价同样低于下轨超过该距离。
  - **做空**：上一根 K 线最高价高于上轨超过 `DistancePoints`，且当前买价同样高于上轨超过该距离。
- **过滤条件**：
  - 同一时间只允许一笔持仓，持仓未平仓时不再开新单。
  - 若 `MaxSpreadPoints` 大于 0，新单提交前需要点差低于该阈值。

## 风险控制
- **下单手数**：`TradeVolume` 参数控制每次市价单的数量。
- **止损**：`StopLossPoints` 乘以品种最小跳动价差转换为绝对价格距离。
- **止盈**：可选的 `TakeProfitPoints`，设为 0 表示关闭。
- **移动止损**：可选的 `TrailingStopPoints`，设为 0 表示关闭。
- **保护机制**：`StartProtection` 辅助函数以市价单方式应用所有离场规则，与 MetaTrader 行为保持一致。

## 参数
- `TradeVolume = 1m`
- `DistancePoints = 140`
- `EnvelopePeriod = 3`
- `EnvelopeDeviationPercent = 0.05m`
- `StopLossPoints = 250`
- `TakeProfitPoints = 0`
- `TrailingStopPoints = 120`
- `MaxSpreadPoints = 25`
- `CandleType = TimeFrame(5 minutes)`

## 标签
- 分类：均值回归
- 方向：双向
- 指标：WeightedMovingAverage
- 止损：是（固定 + 移动）
- 周期：日内 (M5)
- 复杂度：入门
- 风险等级：中等
- 季节性：无
- 神经网络：无
- 背离：无
