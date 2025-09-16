# Ichi Oscillator 策略

## 概述
- 将 MetaTrader 5 专家顾问 **Exp_ICHI_OSC** 转换到 StockSharp 高层 API。
- 在可配置的蜡烛序列上运行，通过 Ichimoku 线构建的振荡指标生成交易信号。
- 原始振荡值计算公式为 `((Close - SenkouA) - (Tenkan - Kijun)) / Step`，随后使用可选的均线方法进行平滑处理。
- 原 MQL 版本中的资金管理与滑点控制被 StockSharp 的仓位与成交量管理所替代。

## 参数
| 参数 | 说明 |
| --- | --- |
| `CandleType` | 用于所有指标计算的蜡烛周期。 |
| `IchimokuBase` | 基础周期，派生出 Tenkan (`base * 0.5`)、Kijun (`base * 1.5`) 与 Senkou B (`base * 3`) 的长度。 |
| `Smoothing Method` | 振荡器的平滑方式：`Simple`、`Exponential`、`Smoothed`、`Weighted`、`Jurik`、`Kaufman`。 |
| `Smoothing Length` | 所选平滑方法的周期。 |
| `Smoothing Phase` | 兼容性参数（保留自 MQL 版本，目前在内置平滑方法中未使用）。 |
| `Signal Bar` | 相对于最新完成蜡烛向后读取振荡颜色的偏移条数（默认 `1`）。 |
| `Enable Buy Entries / Enable Sell Entries` | 是否允许开多 / 开空。 |
| `Enable Buy Exits / Enable Sell Exits` | 是否允许平多 / 平空。 |
| `Stop Loss (points)` | 以价格最小步长表示的止损距离。 |
| `Take Profit (points)` | 以价格最小步长表示的止盈距离。 |
| `Order Volume` | 市价单使用的基础交易量。 |

## 交易逻辑
1. 订阅所选蜡烛数据，并使用派生周期计算 Tenkan、Kijun、Senkou A。
2. 依据价格、Senkou A、Tenkan、Kijun 的差值构建振荡器，并用指定的平滑方法处理。
3. 为每个平滑后的值赋予颜色：
   - `0` — 振荡器大于零并向上。
   - `1` — 振荡器大于零但回落。
   - `2` — 中性状态（零附近或持平）。
   - `3` — 振荡器小于零并继续走低。
   - `4` — 振荡器小于零但回升。
4. 读取两个颜色：`SignalBar + 1`（上一颜色）与 `SignalBar`（当前颜色）。
   - 当上一颜色为 `0` 或 `3` 时，如果允许平空则先平仓，且在当前颜色为 `2`、`1` 或 `4` 时开多。
   - 当上一颜色为 `4` 或 `1` 时，如果允许平多则先平仓，且在当前颜色为 `0`、`1` 或 `3` 时开空。
5. 所有订单均使用设定的交易量。策略不会叠加方向：在同一根蜡烛内先执行平仓逻辑，再评估开仓信号。

## 风险控制
- 通过 `StartProtection` 启动止损与止盈，单位均为价格步长。
- 默认不启用追踪止损或分批离场。

## 备注
- 原策略的复杂资金管理与滑点设置被移除，仅保留固定交易量参数。
- StockSharp 暂不提供 JurX、ParMA、VIDYA、T3 等平滑方法，请在可选列表中选择最接近的替代方案。
- 日志中的信号时间为蜡烛收盘时间加上完整的一个周期，用以复现 MQL 中 `TimeShiftSec` 的行为。
