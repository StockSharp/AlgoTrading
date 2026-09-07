# 假突破陷阱策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在价格短暂越过边界后重新回到此前二十根K线区间内时交易。两个分方向门控限制重复入场，SMA 则为持仓提供即时离场条件。

![schema](schema.svg)

## 策略概览

- 已完成的一分钟K线被拆分为 High、Low 和 Close 数据流。
- Highest(20) 与 Lowest(20) 后接 Shift = 1 的 Previous value，得到不包含当前K线的区间。
- High 高于此前区间上沿且 Close 回到其下方表示向上假突破；Low 的镜像条件表示向下假突破。
- 卖出侧 Flag 与买入侧 Flag 共用一个等待 500 根已完成K线的 N values；每一侧在下一次共同重置前只放行一次。
- 仅在空仓时以固定数量 1 市价入场，SMA(20) 条件用相同数量减少持仓。

## 入场与出场规则

- **做多入场**: Low 低于此前二十根K线最低点，Close 回到该边界上方，并且买入侧冷却门控接受事件。仅在空仓时市价买入一单位。
- **做空入场**: High 高于此前二十根K线最高点，Close 回到该边界下方，并且卖出侧冷却门控接受事件。仅在空仓时市价卖出一单位。
- **离场**: Close 低于 SMA(20) 时将多仓减少一单位，Close 高于 SMA(20) 时将空仓减少一单位。离场立即执行，不经过入场冷却。图中没有止损或止盈。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles Series | 00:01:00 | 用于区间、信号和离场计算的已完成一分钟K线。 |
| Highest Length | 20 | 滚动上边界包含的K线最高价数量。 |
| Highest Source | Not set | 未选择其他指标输入字段；Candle high 直接连接。 |
| Lowest Length | 20 | 滚动下边界包含的K线最低价数量。 |
| Lowest Source | Not set | 未选择其他指标输入字段；Candle low 直接连接。 |
| SMA Length | 20 | 离场移动平均包含的收盘价数量。 |
| SMA Source | Not set | 未选择其他指标输入字段；Candle close 直接连接。 |
| Cooldown N | 500 | 共同冷却重置信号发出前消耗的已完成K线数量。 |
| Entry Volume | 1 | 每次入场和减仓离场使用的固定市价数量。 |

## 图表详情

- Highest 和 Lowest 分别接收数值 High 与 Low，SMA 接收 Close；三个指标都只输出已形成值。
- Previous value 将两个区间指标延后一条更新，因此被检测K线不会参与自身边界。
- 最终计算节拍会在K线字段、指标、仓位和比较结果全部刷新后，才送入两个假突破 AND 门。
- 每个未经门控的假突破事件都会启动共用 N values。经过随后 500 根已完成K线后，其输出重置两个 Flag；在此之前，各 Flag 抑制本方向的重复事件。
- OpenPosition 在已有持仓时阻止新订单。由于冷却分支位于仓位动作之前，此时的假突破事件仍可能启动冷却。
- 两个 SMA 离场门检查仓位方向，并使用数量为 1 的 ReduceOnly 市价动作。Chart 接收K线、两个此前区间边界、SMA 以及全部入场和离场成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
