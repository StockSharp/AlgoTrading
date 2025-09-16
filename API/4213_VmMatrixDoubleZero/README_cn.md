# VmMatrix Double Zero

## 概述
VmMatrix Double Zero 是 MetaTrader 4 策略 `vMATRIXDoubleZero` 的 StockSharp 移植版本。原始策略通过将上一根完结蜡烛的收盘价四舍五入至小数点后两位，寻找价格对该“整数位”水平的突破。移植版本完整保留了 EA 的分层过滤结构：可配置的多蜡烛偏向比较、可选的成交量与区间检测、ATR 加速度开关以及辅助摆动强度过滤器。此外，还可以要求日线 CCI 确认方向，并提供基于小时 ATR 统计的动态止盈补偿。

策略只会在用户设定的终端时间窗口内交易，长/短方向可分别启用或禁用。止损与止盈在策略内部管理，并包含对原始 trailing stop 机制的近似实现：当启用跟踪止损时，止盈距离会自动放大，使持仓主要通过移动止损退出。

## 策略逻辑
### 趋势判定
* **整数位突破**——核心信号比较最近两根已完成蜡烛的收盘价与前一根收盘价的两位小数四舍五入值。做多条件为 `Close[2] < round(Close[1], 2)` 且 `Close[1] > round(Close[1], 2)`；做空条件相反。
* **矩阵过滤器（可选）**——启用后，会根据 `LongK1…LongK6`（做多）或 `ShortK1…ShortK6`（做空）指定的六根历史蜡烛计算偏差 `Close - (High + Low) / 2`。要求第一个偏差大于第二个，第三个偏差大于乘以系数的第四个（`LongQc`/`ShortQc`），第五个偏差大于乘以另一系数的第六个（`LongQg`/`ShortQg`）。

### 附加过滤
* **交易时段**——只有当当前处理蜡烛的收盘小时数位于 `StartHour` 与 `EndHour` 区间内时才会评估信号。
* **成交量过滤**——若开启，上一根蜡烛的总成交量必须大于 `MinimumVolume`。
* **区间压缩**——最近 `RangeBars` 根蜡烛的最高价与最低价之差需要小于 `RangeThresholdPips` 个点。
* **ATR 加速度**——比较当前 ATR（长度 `AtrPeriod`）与 `AtrShift` 根蜡烛之前的 ATR 值，仅在 ATR 上升时接受信号，复现原 EA 的 VSA 开关。
* **二级摆动过滤**——开启后，根据 `SecondaryPivot` 定义的间隔，对高低点差值进行加权求和。做多要求结果为正，做空要求结果为负。权重 `Xb2`、`Xs2`、`Yb2`、`Ys2` 以 50 为基准，与原始参数一致。
* **日线 CCI 确认**——可选条件，要求最新日线 CCI（周期 `DailyCciPeriod`）在做多时为正、做空时为负。

### 仓位管理
* **下单手数**——使用 `OrderVolume` 并根据标的的最小成交量步长归一化。如果已经有反向仓位且 `CloseOnBiasFlip` 为真，则先行平仓后再尝试进场；由于移植版本基于净持仓，无法同时持有对冲仓位。
* **初始止损/止盈**——`LongStopLossPips`/`ShortStopLossPips` 与 `LongTakeProfitPips`/`ShortTakeProfitPips` 以点数定义距离，并根据自动识别的点值换算为价格。动态止盈补偿（见下）可在需要时加入。
* **动态止盈**——启用 `UseDynamicTakeProfit` 时，会按权重叠加多项小时 ATR 数据：ATR(1) 的变动、当前 ATR(1)、ATR(25) 以及相隔 `SwingPivot` 根蜡烛的最高价差。权重 `WeightSn1…WeightSn4` 以 50 为中性，与 EA 中的 `TPb()` 逻辑一致。
* **跟踪止损**——当 `UseTrailingStop` 打开时，止损会在价格距离当前止损约两倍初始距离后向前推进，同时止盈距离乘以 10，以模拟原策略让 trailing stop 主导离场的做法。
* **防护性退出**——每根完成的蜡烛都会检测止损或止盈是否被触发，并以市价平仓。若启用 `CloseOnBiasFlip`，当出现反向偏向时也会立即平仓。

## 参数
下表概述了主要参数（除特别说明外均可用于优化）：

| 分组 | 参数 | 说明 |
| --- | --- | --- |
| General | `StartHour` / `EndHour` | 按终端时间定义的交易窗口（含端点）。 |
| General | `OrderVolume` | 基础下单数量，自动对齐到成交量步长。 |
| General | `UseTrailingStop` | 启用近似的跟踪止损，并将止盈距离放大以模拟 EA 行为。 |
| General | `CloseOnBiasFlip` | 为真时，在进场前先平掉反向持仓。 |
| Long / Short | `EnableLongs` / `EnableShorts` | 是否处理多头或空头信号。 |
| Long / Short | `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | 止损/止盈距离（点）。 |
| Filters | `UseBiasFilter` 及 `LongK1…LongK6`, `ShortK1…ShortK6`, `LongQc`, `LongQg`, `ShortQc`, `ShortQg` | 配置矩阵偏向比较。 |
| Filters | `UseRangeFilter`, `RangeBars`, `RangeThresholdPips` | 限制在高波动区间内开仓。 |
| Filters | `UseVolumeFilter`, `MinimumVolume` | 需要前一根蜡烛的成交量达到阈值。 |
| Filters | `UseVsaFilter`, `AtrPeriod`, `AtrShift` | ATR 必须相对 `AtrShift` 根蜡烛前有所增加。 |
| Filters | `UseSecondaryFilter`, `Xb2`, `Xs2`, `Yb2`, `Ys2`, `SecondaryPivot` | 基于高低点的附加摆动过滤器。 |
| Filters | `UseDailyCciFilter`, `DailyCciPeriod` | 日线 CCI 确认方向。 |
| Take Profit | `UseDynamicTakeProfit`, `WeightSn1…WeightSn4`, `SwingPivot` | 控制动态止盈补偿。 |
| General | `CandleType` | 策略使用的主要蜡烛类型（时间框架）。 |

## 其他说明
* 点值根据 `Security.PriceStep` 自动推断。对于 5 位或 3 位报价的外汇品种，会自动乘以 10，与 MQL 中 `Digits`/`Point` 的处理一致。
* 策略会订阅三个数据流：工作时间框架、小时线（用于 ATR）和日线（用于 CCI）。请确保数据源可以提供所有所需的时间框架。
* StockSharp 采用净持仓模型，无法在同一标的上同时持有多头与空头。若需要与原 EA 相似的快速反手，请开启 `CloseOnBiasFlip`。
* 跟踪止损的实现是近似的。原 EA 使用实时点差调整触发阈值，而本移植在价格距离当前止损约两倍初始距离时推进止损，可在无点差信息的情况下取得类似效果。
