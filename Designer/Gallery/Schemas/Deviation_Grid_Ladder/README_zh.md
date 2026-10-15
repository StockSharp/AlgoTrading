# EMA偏差网格阶梯策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图把C#的EMA(30)/StandardDeviation(14)均值回归信号与相邻README的网格思想结合。1.5σ偏差启动可立即成交的近端限价和2.5σ远端加仓挂单，仓位按源代码的EMA ± 0.5σ回归边界退出。

![schema](schema.svg)

## 策略概览

- 已完成五分钟回放K线更新EMA(30)、StandardDeviation(14)，并在两个指标更新后释放同步收盘价。
- 向下穿越EMA − 1.5σ且Position <= 0时，在−1.5σ和−2.5σ登记买单；Position >= 0时上侧逻辑对称。
- 近端层在确认穿越时通常已穿过市场并立即成交，远端层等待更强走势。
- 多头在Close > EMA + 0.5σ时退出，空头在Close < EMA − 0.5σ时退出，与代码一致。
- 每次均值回归退出撤销两侧远端订单，反向信号也会移除上一侧的旧远端层。

## 入场与出场规则

- **做多入场**: 收盘价向下穿越EMA − 1.5σ且Position <= 0时，在EMA − 1.5σ与EMA − 2.5σ各登记一单位买入限价。
- **做空入场**: 收盘价向上穿越EMA + 1.5σ且Position >= 0时，在EMA + 1.5σ与EMA + 2.5σ各登记一单位卖出限价。
- **离场**: 多头Close > EMA + 0.5σ、空头Close < EMA − 0.5σ时触发市价ClosePosition。它会自动按完整当前仓位定量，包括两层均成交的情况。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 已完成K线周期；五分钟是对C#四小时默认值的回放适配。 |
| EMA Length | 30 | 中心EMA周期，也是两个真实C#参数之一。 |
| Standard Deviation Length | 14 | 宽度估计周期；14在C#中是字面量。 |
| Near Entry Deviation | 1.5σ | 第一入场倍数；1.5是源代码条件中的字面量。 |
| Far Grid Deviation | 2.5σ | 来自README的第二挂单层，执行代码中不存在。 |
| Mean-Reversion Exit Deviation | 0.5σ | 源代码退出的回归边界；0.5是C#字面量。 |
| Volume per Rung | 1 | 每个近端和远端限价层的独立数量。 |

## 图表详情

- C#真正的StrategyParam只有EmaLength和CandleType。StandardDeviation周期14及1.5、0.5倍数是字面量；图中公开它们仅为学习。
- C#默认四小时K线；五分钟是回放适配，使一个月内有足够的指标形成和偏差事件。
- 可执行C#只有一个1.5σ入场阈值，尽管目录名为Three Level Grid。2.5σ第二层明确来自README，而非隐藏代码行为。
- 源代码用两张即时市价单反转仓位。本图保留Position <= 0 / >= 0，但近端可能先平旧仓，远端挂单稍后完成反转。
- Crossing使每次越界只建立一组阶梯。若退出后不撤销远端加仓单，它日后可能开启无人管理的仓位。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
