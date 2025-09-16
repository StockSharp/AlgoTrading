# Starter V6 Mod 策略（StockSharp 版本）

## 概述

**Starter V6 Mod** 是将 MetaTrader 5 专家顾问 `Starter_v6mod` 迁移到 StockSharp 高阶 API 的结果。原系统以 Laguerre RSI、双指数均线、CCI 与网格式仓位控制为核心。本转换在 StockSharp 平台中复刻了多层过滤、分批加仓、动态仓位与保护逻辑。

## 交易逻辑

### 指标组合

* **Laguerre RSI 代理**：使用 14 周期 RSI 并标准化到 0-1，模拟原 Laguerre RSI，参数 `LevelDown` 与 `LevelUp` 定义超卖/超买。
* **慢速 EMA（120）与快速 EMA（40）**：基于蜡烛的中位价计算，两者的价差用于判断趋势方向，`AngleThreshold` 以最小变动单位衡量价差阈值。
* **CCI（14）**：确认动量方向，做多需 CCI < 0，做空需 CCI > 0。

### 入场条件

1. 依据 EMA 价差确定趋势方向：
   * `慢 EMA - 快 EMA < -AngleThreshold` 时仅允许做多；
   * `慢 EMA - 快 EMA > AngleThreshold` 时仅允许做空；
   * 介于阈值内视为震荡，不开新仓。
2. 在趋势方向允许的情况下，需同时满足振荡器与动量过滤：
   * 多头：Laguerre 代理 < `LevelDown`，慢 EMA、快 EMA 均低于其前值，且 CCI < 0；
   * 空头：Laguerre 代理 > `LevelUp`，慢 EMA、快 EMA 均高于其前值，且 CCI > 0。
3. **网格加仓**：若已有同向仓位，当前价需低于所有多单最低价 `GridStepPips`（或高于所有空单最高价）方可加仓。
4. **仓位数量**：同向网格持仓数不能超过 `MaxOpenTrades`。

### 离场条件

* **Laguerre 反向信号**：多头在指标上穿 `LevelUp` 时平仓，空头在下穿 `LevelDown` 时平仓。
* **止损/止盈**：以点值设置，按品种的最小跳动转换成价格差，兼容 3/5 位点差品种。
* **追踪止损**：当浮盈超过 `TrailingStopPips + TrailingStepPips` 后开始跟随，偏移量为 `TrailingStopPips`。
* **周五保护**：18:00 后不再开仓，20:00 强制平仓。

### 资金管理

* **仓位大小**：可固定（`UseManualVolume=true`）或按风险计算，风险模式下 `Volume = Equity * RiskPercent / StopLoss距离`。
* **权益阈值**：权益低于 `EquityCutoff` 时停止开新仓。
* **日内亏损限制**：同一自然日内亏损平仓次数达到 `MaxLossesPerDay` 后停止交易。
* **递减加仓**：每次亏损后将下一笔仓位除以 `DecreaseFactor^亏损次数`。

## 实现说明

* 使用高阶 API `SubscribeCandles().Bind(...)` 绑定蜡烛及指标数据，保证仅在完整蜡烛上决策。
* 因缺乏原 Laguerre RSI 实现，采用标准 RSI 映射至 0-1 区间作为替代，阈值保持一致。
* EMA 角度过滤通过比较快慢 EMA 价差与 `AngleThreshold`（以 tick 表示）来实现，与原 `emaangle` 自定义指标效果对应。
* 止损、追踪逻辑在 `ProcessCandle` 中手动更新，以匹配 MQL 中对持仓保护的逐笔修改。
* 通过记录平均持仓价、最低/最高持仓价与追踪阈值，在 StockSharp 聚合仓位模型下复现网格行为。

## 参数

| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `UseManualVolume` | `false` | 是否使用固定仓位。 |
| `ManualVolume` | `1` | 手动模式下的开仓量。 |
| `RiskPercent` | `5` | 风险百分比（自动仓位模式）。 |
| `StopLossPips` | `35` | 止损点数。 |
| `TakeProfitPips` | `10` | 止盈点数。 |
| `TrailingStopPips` | `0` | 追踪止损点数（0 表示关闭）。 |
| `TrailingStepPips` | `5` | 启动追踪前需达到的额外点数。 |
| `DecreaseFactor` | `1.6` | 亏损后缩减仓位的系数。 |
| `MaxLossesPerDay` | `3` | 每日允许的最大亏损次数。 |
| `EquityCutoff` | `800` | 停止交易的权益下限。 |
| `MaxOpenTrades` | `10` | 同向最大网格仓位数。 |
| `GridStepPips` | `30` | 网格加仓所需的最小价差。 |
| `LongEmaPeriod` | `120` | 慢 EMA 周期。 |
| `ShortEmaPeriod` | `40` | 快 EMA 周期。 |
| `CciPeriod` | `14` | CCI 周期。 |
| `AngleThreshold` | `3` | EMA 价差阈值（tick）。 |
| `LevelUp` | `0.85` | Laguerre 上界。 |
| `LevelDown` | `0.15` | Laguerre 下界。 |
| `CandleType` | `15m` | 使用的蜡烛周期。 |

## 使用建议

1. 将 `CandleType` 设置为与原 MT5 参数一致的时间框架（EA 常用于 15 分钟）。
2. 采用风险仓位模式时，应根据品种波动调整 `StopLossPips`，避免仓位异常。
3. 确认交易所时间，内置的周五平仓逻辑以服务器时间为准。
4. 建议开启图表绘制以观察 EMA、RSI 代理、CCI 与成交点，便于调试与优化。
5. 若从 MT5 迁移参数，需要注意 RSI 代理与 Laguerre RSI 仍有细微差异，可微调阈值。

## 文件

* `CS/StarterV6ModStrategy.cs` – 策略主代码。
* `README.md` – 英文说明。
* `README_cn.md` – 中文说明（当前文件）。
* `README_ru.md` – 俄文说明。

