# Sunil BB Blast Heikin Ashi Strategy
[English](README.md) | [Русский](README_ru.md)

结合布林带突破和平均足确认的策略。

策略等待价格突破布林带且前一根平均足和普通K线方向一致。仓位使用相反的带作为止损，并根据风险回报比设置目标。

## 细节

- **入场条件**：价格突破布林带且前一根平均足与K线方向相同。
- **多空方向**：通过 `Direction` 配置。
- **出场条件**：基于布林带的止盈或止损。
- **止损**：布林带和风险回报比。
- **默认值**：
  - `BollingerPeriod` = 19
  - `BollingerMultiplier` = 2m
  - `RiskRewardRatio` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Direction` = TradeDirection.Both
  - `SessionBegin` = 09:20:00
  - `SessionEnd` = 15:00:00
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：Bollinger, HeikinAshi
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
