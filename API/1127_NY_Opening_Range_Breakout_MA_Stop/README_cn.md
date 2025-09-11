# NY Opening Range Breakout - MA Stop 策略
[English](README.md) | [Русский](README_ru.md)

该策略在纽约时间 9:30-9:45 的开盘区间突破后入场，可选择使用均线作为止盈/过滤。若上一根K线突破区间且未超过截止时间，并且符合均线过滤，则在下一根K线上入场。

## 详情

- **入场条件**：
  - 前一根K线收盘价突破区间高点（做多）或低点（做空），且在截止时间之前。
  - 当前K线为突破后的第一根，并在启用时满足均线过滤。
- **多空方向**：通过 `TradeDirection` 配置。
- **出场条件**：
  - 止损放在开盘区间的另一侧。
  - 止盈根据 `TakeProfitType`：固定风险回报、均线反向或两者同时。
- **止损**：有，位于区间边界。
- **默认值**：
  - `CutoffHour` = 12
  - `CutoffMinute` = 0
  - `TradeDirection` = LongOnly
  - `TakeProfitType` = FixedRiskReward
  - `TpRatio` = 2.5
  - `MaType` = SMA
  - `MaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：Breakout
  - 方向：可配置
  - 指标：移动平均线
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
