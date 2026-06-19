# Trickerless RHMP 策略（StockSharp 版本）

本策略将 MetaTrader 专家顾问 **Trickerless RHMP** 迁移到 StockSharp 的高级 API。它保留了原始机器人多阶段的入场判定 ——
利用平均趋向指数（ADX）确认趋势、平滑移动平均线判断结构，以及基于 ATR 的头寸管理 —— 并遵循仓库中 `AGENTS.md`
所要求的框架约定。

## 交易逻辑

1. **指标配置**
   - 可调周期的平均真实波幅（ATR），用于衡量波动并设置保护距离。
   - 带有 +DI/-DI 分量的平均趋向指数（ADX），用于过滤趋势强度。
   - 两条平滑移动平均线（SMMA），分别代表快线与慢线过滤器。

2. **趋势评估**
   - 慢速 SMMA 的斜率必须位于 `MinSlopePips` 与 `MaxSlopePips` 之间（以品种的点值衡量）。
   - ADX 需要高于 `AdxThreshold` 且相较上一根蜡烛上升。
   - 收盘价与快速 SMMA 的距离至少达到 `TrendSpacePips` 点，避免在盘整中频繁交易。
   - 多头偏好要求快线在慢线上方、+DI ≥ -DI 且快线向上；空头逻辑对称。

3. **主信号入场**
   - 当多头（或空头）条件满足时，以 `OrderVolume` 的基础手数建立多单（或空单），同时遵守 `MaxNetPositions`
     限制，并在两次入场之间至少等待 `SleepInterval`。
   - 若存在反向净头寸，会先行平仓，保持无对冲状态。

4. **尖峰补仓**
   - 当前蜡烛的高低区间若超过上一根的 `CandleSpikeMultiplier` 倍，并且 ADX 分量支持当前实体方向，则允许按
     `OrderVolume * SpikeVolumeMultiplier` 的仓位在尖峰方向追加一笔交易。

## 风险控制

- 依据 ATR 的止损、止盈与可选追踪止损（`StopLossAtrMultiplier`、`TakeProfitAtrMultiplier`、`TrailingAtrMultiplier`）。
- 会话级保护：当已实现盈亏达到 `DailyProfitTarget`（相对开仓时的权益比例）后，停止新开仓。
- `EmergencyExit` 作为紧急开关，可立即平掉所有仓位。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于分析的时间框架。 | 5 分钟 K 线 |
| `OrderVolume` | 每次下单的基础手数。 | 0.03 |
| `AtrPeriod` | ATR 计算周期。 | 14 |
| `AdxPeriod` | ADX 计算周期。 | 14 |
| `AdxThreshold` | 启动交易所需的最小 ADX。 | 10 |
| `FastMaPeriod` | 快速 SMMA 周期。 | 60 |
| `SlowMaPeriod` | 慢速 SMMA 周期。 | 120 |
| `MinSlopePips` / `MaxSlopePips` | 慢速 SMMA 允许的斜率区间。 | 2 / 9 |
| `TrendSpacePips` | 收盘价与快线的最小距离（点）。 | 5 |
| `CandleSpikeMultiplier` | 触发尖峰补仓所需的区间倍率。 | 7 |
| `TakeProfitAtrMultiplier` | ATR 倍数止盈。 | 1.0 |
| `StopLossAtrMultiplier` | ATR 倍数止损。 | 1.5 |
| `TrailingAtrMultiplier` | ATR 倍数追踪止损（0 表示禁用）。 | 0 |
| `MaxNetPositions` | 允许的最大净仓位数量。 | 1 |
| `SleepInterval` | 连续入场之间的最小等待时间。 | 24 分钟 |
| `DailyProfitTarget` | 会话盈亏达到该比例后停止开仓。 | 0.045 |
| `AllowNewEntries` | 是否允许新开仓。 | true |
| `SpikeVolumeMultiplier` | 尖峰补仓的手数倍数。 | 1.0 |
| `EmergencyExit` | 设为 true 时立即平仓并停止。 | false |

## 使用说明

- StockSharp 版本使用高级 API 实现，而非 MetaTrader 中逐笔订单的处理；资金管理通过统一的 `Volume` 和 ATR 保护参数
  完成。
- 原 EA 的账户/保证金检查通过 `DailyProfitTarget`、`MaxNetPositions` 与 ATR 规模控制进行近似。
- 平滑均线需要一定的历史数据预热，建议在回测与实盘前提供充足历史。
