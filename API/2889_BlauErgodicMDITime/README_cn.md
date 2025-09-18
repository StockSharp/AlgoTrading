# Blau Ergodic MDI 时间窗口策略

## 概述

**Blau Ergodic MDI Time Strategy** 是 MetaTrader 专家顾问 `Exp_BlauErgodicMDI_Tm.mq5` 的 StockSharp 版本。策略在较高周期的蜡烛图上运行，并完整重现原策略的三个信号模式：**Breakdown**、**Twist** 与 **CloudTwist**。指标计算完全在策略内部完成，通过四层指数移动平均 (EMA) 管道复现 Blau Ergodic MDI 振荡器，同时符合 StockSharp 高层 API 的要求。

指标流水线如下：

1. 使用 `BaseLength` 周期的 EMA 对选定价格进行平滑。
2. 将平滑结果从原始价格中扣除得到差值序列。
3. 依次对差值应用三条 EMA (`FirstSmoothingLength`、`SecondSmoothingLength`、`ThirdSmoothingLength`)。
4. 将中间值（直方图）与最终值（信号线）按品种最小跳动价进行缩放，并将这些值用于交易逻辑。

## 信号模式

### Breakdown 模式

* 分析 `SignalBar` 指定的历史直方图值及其再往前一根的值。
* 若上一根直方图为正、当前检测的历史柱转为非正，则准备做多，并可按需平掉空头。
* 若上一根直方图为负、当前检测柱转为非负，则准备做空，并可按需平掉多头。

### Twist 模式

* 比较直方图斜率的变化。
* 当斜率上升（`SignalBar + 1` 的值小于 `SignalBar + 2`）且当前检测柱高于上一根时，生成做多信号，同时允许平掉空头。
* 当斜率下降（`SignalBar + 1` 的值大于 `SignalBar + 2`）且当前检测柱低于上一根时，生成做空信号，同时允许平掉多头。

### CloudTwist 模式

* 同时使用直方图与信号线。
* 若上一根直方图高于信号线，但当前检测柱跌破信号线，则准备做多并可平空。
* 若上一根直方图低于信号线，但当前检测柱上穿信号线，则准备做空并可平多。

## 交易时段过滤

策略提供与原版一致的交易时段过滤器，通过 `UseTimeFilter`、`StartHour`、`StartMinute`、`EndHour`、`EndMinute` 控制：

* 当开始时间早于结束时间时，交易窗口位于同一天。
* 当开始与结束小时相等时，分钟设置形成该小时内的短窗口。
* 当开始时间晚于结束时间时，交易窗口跨越午夜。

在交易窗口外策略会立即平掉所有仓位，并禁止新的开仓直到窗口重新开启。

## 风险控制

`StopLossPoints` 与 `TakeProfitPoints` 以最小跳动价为单位设置止损与止盈距离。每次开仓后即刻计算保护价格。策略在每根完成的蜡烛上检查价格区间是否触及保护位，一旦触发立刻平仓。

## 价格来源

`PriceMode` 列出与 MetaTrader 指标完全一致的价格选项：

| 模式 | 说明 |
| ---- | ---- |
| Close | 收盘价。 |
| Open | 开盘价。 |
| High | 最高价。 |
| Low | 最低价。 |
| Median | (High + Low) / 2。 |
| Typical | (High + Low + Close) / 3。 |
| Weighted | (High + Low + 2 × Close) / 4。 |
| Simple | (Open + Close) / 2。 |
| Quarter | (Open + High + Low + Close) / 4。 |
| TrendFollow0 | 多头蜡烛取 High，空头取 Low，十字取 Close。 |
| TrendFollow1 | Close 与蜡烛趋势方向极值的平均。 |
| Demark | Demark 价格计算。 |

## 参数

| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `Mode` | Twist | 选择 Breakdown / Twist / CloudTwist 模式。 |
| `PriceMode` | Close | 指标使用的价格。 |
| `BaseLength` | 20 | 原始价格 EMA 周期。 |
| `FirstSmoothingLength` | 5 | 差值第一次平滑 EMA 周期。 |
| `SecondSmoothingLength` | 3 | 差值第二次平滑 EMA 周期。 |
| `ThirdSmoothingLength` | 8 | 差值第三次平滑 EMA 周期。 |
| `SignalBar` | 1 | 信号参考的历史柱偏移量。 |
| `AllowLongEntry` / `AllowShortEntry` | true | 是否允许开多 / 开空。 |
| `AllowLongExit` / `AllowShortExit` | true | 是否允许平多 / 平空。 |
| `UseTimeFilter` | true | 是否启用交易时段过滤。 |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 0/0/23/59 | 交易时段设置。 |
| `StopLossPoints` | 1000 | 止损距离（0 代表关闭）。 |
| `TakeProfitPoints` | 2000 | 止盈距离（0 代表关闭）。 |
| `CandleType` | 4 小时 | 指标使用的蜡烛类型。 |
| `Volume` | 0.1 | 下单数量，对应原策略的 MM 参数。 |

## 交易流程

1. 订阅所选时间框架的蜡烛数据。
2. 在每根完成的蜡烛上更新四级 EMA 管道，并维护最小所需的历史缓冲。
3. 等待历史数据达到要求，再按照所选模式对 `SignalBar` 对应的历史柱进行判断。
4. 若触发离场条件或时段过滤关闭交易，则优先平仓。
5. 只有在信号触发、交易窗口打开且当前仓位方向允许的情况下才开仓。若需要反向，订单数量会覆盖当前持仓并增加设定的下单量。
6. 每根蜡烛都检查止损止盈是否被价格区间触发，并立即执行。

## 其他说明

* 代码全部使用制表符缩进，符合仓库规范。
* `StartProtection()` 在启动时调用一次，以便 StockSharp 的保护机制正确跟踪仓位。
* 仅存储信号所需的最少历史值，不会累积大型集合。
* 指标目前使用 EMA 平滑，若需要其他平滑方法，可通过调整周期来近似 MetaTrader 中的 JJMA、VIDYA 或 AMA 版本。

## 使用步骤

1. 将策略类添加到 StockSharp 解决方案并编译。
2. 设置品种、蜡烛周期、信号模式、交易时段以及风控参数。
3. 将策略连接到提供行情的连接器。
4. 启动策略，程序会自动订阅蜡烛并按上述规则管理委托与仓位。

