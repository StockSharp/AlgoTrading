# Open Oscillator Cloud MMRec 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 专家顾问 **Exp_Open_Oscillator_Cloud_MMRec** 迁移到 StockSharp 高级 API。系统利用 Open Oscillator Cloud 指标的交叉：当前开盘价与滑动窗口内最高点和最低点所在蜡烛的开盘价进行比较，并使用可配置的移动平均线对结果进行平滑处理。

## 策略逻辑

### 指标构建
- 在所选时间框架上建立一个由已完成蜡烛组成的窗口（`Oscillator Period`，默认 20 根）。
- 找到最高价所在的蜡烛并记录其开盘价，同时找到最低价所在的蜡烛并记录其开盘价。
- 对当前蜡烛计算两个原始值：
  - **上轨** = 当前开盘价 − 最高价蜡烛的开盘价。
  - **下轨** = 最低价蜡烛的开盘价 − 当前开盘价。
- 使用所选移动平均（`Smoothing Method`、`Smoothing Length`）对两条序列进行平滑，仅支持简单、指数、平滑和加权类型。
- 维护平滑值的历史，并按照 `Signal Bar`（默认 1）延迟信号，与原版 EA 在上一根柱子上执行的行为一致。

### 入场条件
- **做多**：上一根柱子的上轨高于下轨，且延迟后的最新值向下穿越（`upper ≤ lower`）。可通过 `Enable Long Entries` 禁用。
- **做空**：上一根柱子的上轨低于下轨，且延迟后的最新值向上穿越（`upper ≥ lower`）。可通过 `Enable Short Entries` 禁用。

### 出场条件
- **平多**：上一根柱子的上轨低于下轨，表示进入空头区域。由 `Enable Long Exits` 控制。
- **平空**：上一根柱子的上轨高于下轨，表示进入多头区域。由 `Enable Short Exits` 控制。
- **风险控制**：当 `Stop Loss Points` 或 `Take Profit Points` 大于 0 时，价格一旦达到相应的价格步长距离，仓位即自动平仓。

### 委托管理
- 仅使用市价单。在开新仓前会先平掉相反方向的仓位，以保持与 MetaTrader 版本相同的单一仓位模式。
- `Trade Volume` 参数决定每次下单的固定手数。

## 参数
- `Candle Type` – 计算指标时使用的蜡烛时间框架（默认 1 小时）。
- `Oscillator Period` – 滑动窗口长度（默认 20）。
- `Smoothing Method` – 平滑开盘价差的移动平均类型（Simple、Exponential、Smoothed、Weighted）。
- `Smoothing Length` – 平滑移动平均的周期（默认 10）。
- `Signal Bar` – 延迟信号的已完成柱子数量（默认 1）。
- `Enable Long Entries` / `Enable Short Entries` – 是否允许开多 / 开空。
- `Enable Long Exits` / `Enable Short Exits` – 是否允许自动平多 / 平空。
- `Trade Volume` – 每次市价单的数量（默认 1 手）。
- `Stop Loss Points` – 止损距离，单位为价格步长（0 表示关闭，默认 1000）。
- `Take Profit Points` – 止盈距离，单位为价格步长（0 表示关闭，默认 2000）。

## 实现说明
- 只保留原策略中常用的平滑方式。JJMA、T3、VIDYA、AMA 等特殊算法未移植，因为 StockSharp 已提供足够的替代方案。
- 仅在 `CandleStates.Finished` 事件上评估信号，避免使用未完成的数据。
- 策略内部保存平滑值历史，而不是访问指标缓冲区，符合 StockSharp 建议的高层工作流。
- 当仓位归零时自动清除保护性价格，防止旧的止损或止盈再次触发。

## 默认行为
- 支持双向交易，并通过延迟确认降低噪声。
- 使用固定仓位规模（`Trade Volume`），并保留与 MetaTrader 版本相同的止损和止盈机制。
- 适合作为实验不同平滑方式或叠加过滤器的模板。
