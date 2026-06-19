# Fibo Arc Momentum 策略

## 概述

该策略是 MetaTrader 智能交易系统 "FiboArc"（目录 `MQL/24924`）在 StockSharp 平台上的移植版本。原始 EA
通过多重动量过滤和斐波那契弧线突破构建交易逻辑。StockSharp 版本保留同样的思路，并使用高层蜡烛 API
实现：

* 两条线性加权均线 (`FastMaPeriod`, `SlowMaPeriod`) 判断趋势方向；
* Momentum 指标检测与 100 的偏离度，过滤掉弱势行情；
* MACD 直方图确认趋势并跟踪最新的交叉；
* 通过 `TrendAnchorLength` 和 `ArcAnchorLength` 选定的两根锚定蜡烛的开盘价，重建简化的斐波那契弧线，
  取代 MetaTrader 版本基于图形对象的检查。

策略仅在蜡烛收盘后做出决策，以避免任何未来数据泄漏，并且更贴合原始 EA 的行为。

## 指标与数据流

策略根据 `CandleType` 订阅一条蜡烛流，并通过 `SubscribeCandles(...).BindEx(...)` 将每根收盘蜡烛输入到以下
指标：

| 指标 | 用途 | 默认设置 |
|------|------|----------|
| LinearWeightedMovingAverage（快线） | 判定短期趋势及入场节奏 | `FastMaPeriod = 6`，典型价 |
| LinearWeightedMovingAverage（慢线） | 过滤大级别趋势 | `SlowMaPeriod = 85`，典型价 |
| Momentum | 计算与 100 的偏离量，确认趋势力度 | `MomentumPeriod = 14` |
| MovingAverageConvergenceDivergenceSignal | 确认趋势并识别交叉 | `MacdFastPeriod = 12`, `MacdSlowPeriod = 26`, `MacdSignalPeriod = 9` |

所有指标都只处理最终数值，不需要手动调用 `GetValue()`，完全符合仓库规范。

## 斐波那契弧线的重建

在 MetaTrader 中，策略绘制图形对象并调用 `ObjectGetValueByShift` 读取弧线值。StockSharp 版本通过数值方法
模拟同样的效果：

1. 策略维护一个滚动的已完成蜡烛列表 `_history`；
2. `TrendAnchorLength` 指定第一根锚定蜡烛，`ArcAnchorLength` 指定第二根锚定蜡烛；
3. 以两根锚定蜡烛的开盘价做线性插值，并乘以 `FibonacciRatio`（默认 0.618），得到当前弧线水平；
4. 比较前一根蜡烛的开盘价与上一条弧线水平，以及当前蜡烛的开盘价与最新水平。如果出现自下而上的穿越
   (`fibCrossUp`) 或自上而下的穿越 (`fibCrossDown`)，即可视为突破信号。

这种处理方式避免了对图形对象的依赖，同时保留了原始策略的核心思想。

## 交易规则

### 做多条件

满足以下全部条件时开多：

1. `fibCrossUp` 表明价格向上突破斐波那契弧线；
2. 快速 LWMA 高于慢速 LWMA；
3. Momentum 与 100 的偏差不少于 `MomentumThreshold`；
4. MACD 主线位于信号线上方，或刚刚发生金叉；
5. 当前没有多头敞口（`Position <= 0`）。

下单量为 `Volume` 加上当前空头仓位的绝对值，从而支持直接反向。

### 做空条件

做空逻辑与做多对称：

1. `fibCrossDown` 表示向下突破；
2. 快速 LWMA 低于慢速 LWMA；
3. Momentum 偏差大于阈值；
4. MACD 主线位于信号线下方或出现死叉；
5. 当前没有多头敞口。

### 离场规则

* 趋势或 MACD 条件反转；
* 出现相反方向的斐波那契突破信号；
* 触及动态止损或止盈。

所有离场都使用市价单执行，与原始 EA 的即时平仓方式保持一致。

## 风险控制

策略实现了原 EA 的主要风险管理功能：

* `StopLossDistance`、`TakeProfitDistance`：按价格单位定义初始止损与止盈距离；
* `EnableBreakEven`、`BreakEvenTrigger`、`BreakEvenOffset`：控制移动到保本价；
* `EnableTrailing`、`TrailingTrigger`、`TrailingDistance`：实现基于蜡烛的跟踪止损。

策略在内部跟踪入场价、止损与止盈位置，并在 `OnNewMyTrade` 回调中使用实际成交价重新计算这些水平。

## 参数一览

| 参数 | 说明 |
|------|------|
| `CandleType` | 主计算所使用的蜡烛类型及周期。 |
| `FastMaPeriod`, `SlowMaPeriod` | 快速与慢速 LWMA 的周期。 |
| `MomentumPeriod`, `MomentumThreshold` | Momentum 指标设置。 |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD 参数。 |
| `TrendAnchorLength`, `ArcAnchorLength`, `FibonacciRatio` | 控制斐波那契弧线的重建方式。 |
| `StopLossDistance`, `TakeProfitDistance` | 初始止损/止盈距离（绝对价格单位）。 |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | 保本移动配置。 |
| `EnableTrailing`, `TrailingTrigger`, `TrailingDistance` | 跟踪止损配置。 |

所有参数均通过 `StrategyParam<T>` 暴露，必要时可用于优化。默认值保持与原 EA 一致。

## 使用方法

1. 将策略附加到目标标的，并设置 `Volume` 为期望的仓位规模；
2. 根据市场特点调整时间框架、均线长度以及斐波那契参数；
3. 启动策略。所有决策都基于收盘蜡烛，无需处理盘中数据；
4. 如环境支持，可查看附带的快/慢 LWMA 与 MACD 图表，用于可视化验证。

根据任务要求，本策略暂不提供 Python 版本，测试用例也未做任何修改。
