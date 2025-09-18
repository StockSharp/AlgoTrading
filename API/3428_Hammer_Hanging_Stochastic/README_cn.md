# Hammer Hanging Stochastic
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 顾问 "Expert_AH_HM_Stoch" 迁移到 StockSharp 高级 API。它结合锤子线/上吊线形态与随机指标确认，用于捕捉强趋势后的反转机会。

所有决策均基于已完成的 K 线。策略使用随机指标 %D 线作为过滤器，并在动能离开极值区域时平仓。

## 详情

- **入场条件**：
  - 多头：出现锤子线且上一根柱子的随机指标 %D 低于超卖阈值。
  - 空头：出现上吊线且上一根柱子的随机指标 %D 高于超买阈值。
- **方向**：支持多空双向。
- **离场**：当随机指标 %D 向上/向下突破可配置的恢复与极值水平时退出持仓。
- **止损**：通过 `StartProtection()` 启动（默认使用账户级别的保护参数，可按需调整）。
- **默认参数**：
  - `CandleType` = TimeSpan.FromHours(1)
  - `StochPeriodK` = 15
  - `StochPeriodD` = 49
  - `StochPeriodSlow` = 25
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `ExitLowerLevel` = 20
  - `ExitUpperLevel` = 80
  - `MaxBodyRatio` = 0.35
  - `LowerShadowMultiplier` = 2.5
  - `UpperShadowMultiplier` = 0.3
- **过滤器**：
  - 分类：形态 + 振荡器确认
  - 方向：双向
  - 指标：蜡烛形态、Stochastic
  - 止损：通过 `StartProtection` 提供
  - 复杂度：中等
  - 周期：波段/日内（默认 1 小时）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

## 工作流程

1. 通过 `BindEx` 订阅所选蜡烛序列和随机指标，使用 StockSharp 高级 API。
2. 根据实体与影线比例识别锤子线和上吊线形态。
3. 使用上一根柱子的随机指标 %D 确认入场信号。
4. 当随机指标离开超卖/超买区域时平仓，复刻原始 MQL 策略的退出逻辑。
5. 在可用的图表区域中绘制蜡烛、随机指标以及策略自身的成交记录。
