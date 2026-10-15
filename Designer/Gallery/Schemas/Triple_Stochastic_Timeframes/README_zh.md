# 三周期随机指标策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表组合 60 分钟、15 分钟和 5 分钟已形成的 Stochastic(5,3) 动量。它每小时同步三组 %K-%D 差值，并在两个较高周期方向一致时交易五分钟动量转折。

![schema](schema.svg)

## 策略概览

- 三个已完成蜡烛流分别提供 60 分钟、15 分钟和 5 分钟数据，并可由更小周期构建。
- 每个数据流计算 Stochastic %K(5)，再用 SMA(3) 平滑 %K 得到 %D，最后计算 %K 减 %D。
- 小时差值触发对三个数据流最新值的采样；每根小时蜡烛结束时，Sync 输出一组完整的三个值。
- 前一个已同步的五分钟差值用于识别零线转折，当前仓位则防止持仓超过一个单位。
- 两个动作均为一单位市价操作，Strategy trades 将每笔成交发送到图表。

## 入场与出场规则

- **做多入场**: 当前一个入场差值大于零、当前入场差值小于或等于零、两个较高周期差值均大于零且仓位不是多仓时，以市价买入一个单位。
- **做空入场**: 当前一个入场差值小于零、当前入场差值大于或等于零、两个较高周期差值均小于零且仓位不是空仓时，以市价卖出一个单位。
- **离场**: 图中没有单独的离场分支。一次有效的反向一单位操作会把相反的一单位持仓减至零；之后出现新的有效信号时可开立另一方向。图中没有止损、止盈或交易冷却。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Higher Candles Series | 01:00:00 | 用于较高周期计算和小时决策脉冲的已完成 60 分钟蜡烛。 |
| Higher Stochastic %K Length | 5 | 较高周期 Stochastic %K 的回看长度。 |
| Higher Stochastic %K Source | Not set | 未选择额外的指标输入字段；较高周期蜡烛直接连接。 |
| Higher Stochastic %D SMA Length | 3 | 将较高周期 %K 平滑为 %D 的长度。 |
| Higher Stochastic %D SMA Source | Not set | 未选择额外的指标输入字段；较高周期 %K 直接连接。 |
| Middle Candles Series | 00:15:00 | 用于中间周期计算的已完成 15 分钟蜡烛。 |
| Middle Stochastic %K Length | 5 | 中间周期 Stochastic %K 的回看长度。 |
| Middle Stochastic %K Source | Not set | 未选择额外的指标输入字段；中间周期蜡烛直接连接。 |
| Middle Stochastic %D SMA Length | 3 | 将中间周期 %K 平滑为 %D 的长度。 |
| Middle Stochastic %D SMA Source | Not set | 未选择额外的指标输入字段；中间周期 %K 直接连接。 |
| Entry Candles Series | 00:05:00 | 用于入场周期计算和图表的已完成 5 分钟蜡烛。 |
| Entry Stochastic %K Length | 5 | 入场周期 Stochastic %K 的回看长度。 |
| Entry Stochastic %K Source | Not set | 未选择额外的指标输入字段；入场周期蜡烛直接连接。 |
| Entry Stochastic %D SMA Length | 3 | 将入场周期 %K 平滑为 %D 的长度。 |
| Entry Stochastic %D SMA Source | Not set | 未选择额外的指标输入字段；入场周期 %K 直接连接。 |
| Order Volume | 1 | 两个动作使用的固定市价数量。 |

## 图表详情

- 所有指标仅输出已形成值。SMA(3) 接收对应的 %K 输出，因此每个差值都等于 %K 减去它的三值平均线。
- 小时差值先触发三个数值采样块，再让数值进入 Sync；因此中间和入场采样块提供该时刻最新的可用读数。
- Sync 在输出完整组后清空，并给出三个对齐值。Previous value 块保存一个已同步入场差值，供下一次小时比较使用。
- 仓位与同步决策同时采样。仓位小于或等于零时允许买入，大于或等于零时允许卖出，从而阻止同向加仓。
- 图表接收五个数据流：五分钟蜡烛、三个实时 %K-%D 差值以及全部策略成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
