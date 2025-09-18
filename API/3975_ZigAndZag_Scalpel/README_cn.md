# ZigAndZag Scalpel 策略

## 概述
ZigAndZagScalpelStrategy 是 MetaTrader 4 "ZigAndZag" 方案（文件夹 8304）的 StockSharp 版本。
原始组合包含指标和智能交易系统，两组 ZigZag 协同运作：

* **KeelOver** – 长周期 ZigZag，用于识别主要趋势。
* **Slalom** – 短周期 ZigZag，用于寻找入场突破。

当长周期 ZigZag 转向上升时，策略跟踪最新的 Slalom 低点，等待价格向上突破该枢纽点
若干点位后买入。相反方向：趋势向下、Slalom 出现新高并且价格跌破该高点时开空。
启用 `CloseOnOppositePivot` 后，一旦出现相反的 Slalom 枢纽点即平仓，复刻了原始指标
移除限价箭头的行为。

策略保留了专家顾问中的“新交易日”限制。每天的交易次数受 `MaxTradesPerDay` 控制，
午夜会自动重置，行为与 MQL 代码中的 `newday` 标志一致。

## 工作流程
1. 订阅 `CandleType` 指定的主时间框蜡烛。
2. 启动两条 `ZigZagIndicator`：
   * 深度 = `KeelOverLength`，用于确定趋势方向。
   * 深度 = `SlalomLength`，用于捕捉入场枢纽点。
3. 根据最新的 KeelOver 枢纽判断趋势是向上（低点）还是向下（高点）。
4. Slalom 给出新枢纽时，记录该方向并等待突破。
5. 计算加权价格 `(5×Close + 2×Open + High + Low) / 9`。当价格相对枢纽超过
   `BreakoutDistancePoints`（换算为实际价格单位）且趋势同向时，发出市价单。
6. 若趋势反转或出现相反的 Slalom 枢纽且 `CloseOnOppositePivot` 为真，立即平掉持仓。
7. 每次跨日时重置日内交易计数器。

`DeviationPoints` 与 `Backstep` 为两条 ZigZag 共用，确保与原 MT4 指标的缓冲区结构一致。

## 参数
| 名称 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `CandleType` | `15m` | 构建两条 ZigZag 所用的主时间框。 |
| `KeelOverLength` | `55` | 趋势 ZigZag 的回溯长度（原 KeelOver）。 |
| `SlalomLength` | `17` | 入场 ZigZag 的回溯长度（原 Slalom）。 |
| `DeviationPoints` | `5` | 认定新枢纽所需的最小点差。 |
| `Backstep` | `3` | 相邻枢纽之间最少的条数。 |
| `BreakoutDistancePoints` | `2` | 相对枢纽的突破距离（点）。 |
| `MaxTradesPerDay` | `1` | 每日最大开仓次数，对应原始 `newday` 限制。 |
| `CloseOnOppositePivot` | `true` | 出现相反 Slalom 枢纽时是否立即平仓。 |

所有“点”参数都会乘以 `Security.PriceStep` 转成价格单位；若没有价格步长，默认使用 `1`
以便在测试环境中运行。

## 使用建议
* 策略只发送市价单（`BuyMarket` / `SellMarket`）。如需止损或目标管理，可在外部增加
  风险控制模块。
* 两条 ZigZag 使用同一串蜡烛，请确保数据源支持所选 `CandleType`。
* 保持 `MaxTradesPerDay = 1` 即可复制原策略的“每日一次”逻辑。需要更多机会时可调大
  限制。
* 将 `CloseOnOppositePivot` 设为 `false` 可以让持仓持续到趋势真正改变，而不是响应每个
  短期摆动。

## 与 MT4 版本的差异
* 原 EA 通过限价箭头排队等待突破；本移植使用高层 API 的市价单直接入场。
* 未移植自动仓位管理、止盈止损等逻辑。可结合 StockSharp 的风控组件自行扩展。
* 指标缓冲区 4/5/6 的功能由策略逻辑直接处理，并通过 `DrawIndicator` / `DrawOwnTrades`
  在图表上呈现。

## 推荐扩展
* 新增基于 ATR 或 ZigZag 枢纽的止损、止盈参数。
* 将 `BreakoutDistancePoints` 设为 0，可观察原始枢纽阶梯效果。
* 如需限定交易时间，可结合 `IsFormedAndOnlineAndAllowTrading` 等会话过滤器。
