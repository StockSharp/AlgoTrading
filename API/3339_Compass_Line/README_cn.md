# Compass Line Strategy
[English](README.md) | [Русский](README_ru.md)

该策略复刻 CompassLine 智能交易系统，将两个互补的过滤器结合起来：

* **Follow Line** —— 基于布林带突破的追踪线，可选 ATR 偏移。价格突破带宽后，追踪线沿着突破方向延伸，并在趋势持续期间保持单向移动。
* **Compass** —— 计算一定窗口内最高价与最低价的区间，将中值价格做逻辑变换，再用三角平滑进行二次滤波，得到稳定的多/空状态。

只有当两个过滤器方向一致时才开仓，同时保留时间过滤与防护性止损设置以匹配原始逻辑。

## 细节

- **入场条件**：
  - Follow Line 指向上方（收盘价高于上轨）时做多，指向下方（收盘价低于下轨）时做空，可通过 `UseAtrFilter` 开启 ATR 偏移。
  - Compass 状态（由 `CompassPeriod` 控制）在双重平滑后为正值时做多，为负值时做空。
  - 仅当可选的交易时段过滤器 (`UseTimeFilter` 与 `Session`，格式 HHmm-HHmm) 允许交易时才执行下单。
- **多空方向**：支持双向交易。
- **出场条件**：
  - `CloseMode = None`：持仓直到出现反向信号或止盈/止损触发。
  - `CloseMode = BothIndicators`：Follow Line 与 Compass 同时反转时平仓。
  - `CloseMode = FollowLineOnly`：Follow Line 反向时平仓。
  - `CloseMode = CompassOnly`：Compass 极性变化时平仓。
- **止损/止盈**：当 `TakeProfit`、`StopLoss`（以最小价位步长计）大于零时，会在每次进场后设置。
- **默认值**：
  - `FollowBbPeriod` = 21
  - `FollowBbDeviation` = 1
  - `FollowAtrPeriod` = 5
  - `UseAtrFilter` = false
  - `CompassPeriod` = 30（平滑长度 = round(CompassPeriod / 3)）
  - `CloseMode` = None
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `TakeProfit` = 0
  - `StopLoss` = 0
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器标签**：
  - 类别: Trend
  - 方向: Both
  - 指标: Bollinger Bands, ATR, Triangular moving average
  - 止损: 可选
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: Medium

## 其他说明

- Compass 的平滑长度取 round(`CompassPeriod` / 3)，与原指标的处理方式一致。
- 会话字符串（例如 `0930-1600`）用于限制下单时间，但指标在会话外仍然更新。
- 防护单使用 StockSharp 的高层 API，可与账户风险管理模块协同工作。
