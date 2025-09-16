# EMA WMA RSI

## 概述
EMA WMA RSI 是 MetaTrader 4 专家顾问“EMA WMA RSI”（作者 cmillion）的 StockSharp 版本。原始 EA 在每根 K 线的开盘价上计算指数移动平均线（EMA）与线性加权移动平均线（WMA），并使用相同价格源计算的相对强弱指标（RSI）作为方向过滤器。移植后的策略保留了原有指标逻辑，只在已完成的蜡烛上运作，同时复刻了资金管理选项：可选的反向仓位平仓、以点为单位的止损/止盈，以及三种拖尾止损方式（固定距离、最近分形或近期蜡烛极值）。

策略针对单一品种运行，时间框架由 `Candle Type` 参数决定。所有点值均按 MetaTrader 中的“Point”（最小跳动）换算，因此请在证券信息里填充 `Security.Step`、`Security.PriceStep` 与 `Security.StepPrice` 等元数据，以便正确转换价格距离。

## 交易逻辑
### 指标
* **EMA** – 由 `EMA Period` 控制周期，输入为蜡烛开盘价。
* **WMA** – 周期由 `WMA Period` 决定，同样使用开盘价序列。
* **RSI** – `RSI Period` 控制周期，同样基于开盘价。

指标准备只在蜡烛收盘时更新；为了复现原始 EA 的“新柱执行”行为，策略会保存上一根柱的 EMA/WMA 数值，与当前柱的数值比较。

### 入场条件
* **多头条件**
  1. 当前 EMA 低于 WMA，而上一根柱的 EMA 高于 WMA（向下交叉）。
  2. RSI 大于 50。
  3. 如当前持有空头仓位，在 `Close Counter Trades` 为真时先平掉空头；若关闭此选项则忽略信号直到仓位扁平。
  4. 条件满足后按固定手数或风险百分比下市场买单。
* **空头条件** – 逻辑完全对称：EMA 向上穿越 WMA、上一根柱 EMA 低于 WMA、RSI 小于 50，并根据设置处理已有多头。

### 离场机制
* **初始保护** – `Stop Loss (points)` 与 `Take Profit (points)` 会通过最小跳动转换成绝对价差，设置为 0 表示关闭对应保护。
* **拖尾止损**
  * `Trailing Stop (points)` 大于 0 时采用固定距离拖尾，依据最新收盘价向有利方向收紧。
  * 拖尾距离为 0 时启用自适应模式：
    * `Trailing Source = CandleExtremes`：从最近的已完成蜡烛中寻找满足至少 5 点缓冲的最低价/最高价。
    * `Trailing Source = Fractals`：回溯已确认的比尔·威廉姆斯分形（前后各两根蜡烛），同样要求至少 5 点缓冲。
  * 只有当价格越过开仓价后，拖尾才会启动，贴合原函数 `SlLastBar` 的行为。
* **平仓执行** – 若当根蜡烛的极值触碰拖尾价或止盈价，即以市价平仓并重置内部状态。

### 仓位管理
* `Fixed Volume` 指定固定下单手数（与原 EA 的 `Lot` 参数对应）。
* 将 `Fixed Volume` 设为 0 时启用风险百分比头寸控制。策略会利用有效的止损距离（止损或拖尾）及 `Security.StepPrice` 估算每单位仓位的货币风险，再根据 `Risk %` 分配权益。如果固定手数与风险百分比同时为 0，信号将被忽略。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `EMA Period` | 开盘价 EMA 的周期。 | `28` |
| `WMA Period` | 开盘价 WMA 的周期。 | `8` |
| `RSI Period` | RSI 过滤器周期。 | `14` |
| `Stop Loss (points)` | 以点为单位的止损距离，`0` 表示关闭。 | `0` |
| `Take Profit (points)` | 以点为单位的止盈距离，`0` 表示关闭。 | `500` |
| `Trailing Stop (points)` | 固定拖尾距离；`0` 表示启用自适应拖尾。 | `70` |
| `Trailing Source` | 自适应拖尾源：`CandleExtremes` 使用蜡烛高低点，`Fractals` 使用分形。 | `CandleExtremes` |
| `Close Counter Trades` | 入场前是否先平掉反向仓位。 | `false` |
| `Fixed Volume` | 固定下单手数。设为 `0` 时改用风险百分比。 | `0.1` |
| `Risk %` | 启用风险头寸时使用的权益百分比，需要有效的保护距离。 | `10` |
| `Candle Type` | 主图时间框架。 | `30 分钟蜡烛` |

## 实现细节
* 点值换算依赖 `Security.Step`/`Security.PriceStep` 与 `Security.StepPrice`，请确保品种信息完整。
* 策略仅处理收盘蜡烛，并以开盘价更新指标，符合原始 MQL4 代码的逻辑。
* 拖尾逻辑保留至少 5 点缓冲，避免止损过于靠近当前价格。
* 当关闭反向平仓时，策略始终保持单向净仓，不会同时持有多空。
* 本目录仅包含 C# 版本，暂无 Python 实现。
