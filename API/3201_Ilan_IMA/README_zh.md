# Ilan iMA 策略

## 概述
**Ilan iMA Strategy** 是 MetaTrader 5 顾问 `Ilan iMA.mq5` 的 StockSharp 版本。原始 EA 通过一条带有前移的加权移动平均来
识别趋势，并使用马丁格尔方式分批加仓。本移植版利用 StockSharp 的高级 API：当趋势被移动平均确认后，策略会开仓，
随后每当价格按设定步长逆向运行时继续加仓。整个仓位篮子会在达到止盈、触发追踪止损或命中固定止损时一次性平仓，
从而复刻 MT5 中的资金管理逻辑。

## 交易逻辑
1. 订阅所选周期 (`CandleType`)，并根据参数 `MaMethod`、`MaPeriod`、`PriceMode` 构建移动平均。正的 `MaShift` 会将指标
   向右平移，因此策略会读取历史值来模拟 MT5 的行为。
2. 仅在蜡烛收盘后处理信号和风险控制。
3. 通过比较带有 `MaShift` 偏移的连续四个移动平均值来判断趋势：
   - 值逐个减小表示下行趋势；
   - 值逐个增大表示上行趋势。
4. 当没有持仓篮子时：
   - 若为下行趋势且收盘价高于移动平均值，则以 `StartVolume` 开空；
   - 若为上行趋势且收盘价低于移动平均值，则以 `StartVolume` 开多。
5. 当已有仓位篮子时：
   - 如果价格逆势运行至少 `GridStepPips`，则按 `LotExponent` 放大手数开同向新单，手数受 `LotMaximum` 及交易所限制作约；
   - 策略会维护买入最低价、卖出最高价以及加权平均价，以贴近 MT5 的网格逻辑。
6. 平仓条件：
   - 当包含多笔订单的篮子浮动利润达到 `ProfitMinimum`（账户货币）时，关闭该方向全部仓位；
   - 当浮动利润达到 `TakeProfitPips` 或浮动亏损达到 `StopLossPips` 时，关闭篮子；
   - 当盈利超过 `TrailingStopPips + TrailingStepPips` 时启动追踪止损，每次仅在新增盈利超过 `TrailingStepPips` 时更新。

## 风险与仓位管理
- `StartVolume` 对应 MT5 中的 `StartLots`。追加订单的体积按 `LotExponent` 成倍增长，同时受到 `LotMaximum` 以及
  `Security.MinVolume`、`Security.VolumeStep`、`Security.MaxVolume` 等交易所参数的限制。
- `ProfitMinimum` 还原了 MT5 中“解除锁仓”的逻辑：当篮子盈利达到阈值时，立刻平掉该方向所有仓位。
- `StopLossPips` 与 `TakeProfitPips` 以点值表示，内部会根据 `Security.PriceStep` 换算成真实价格距离。
- 追踪止损只有在盈利超过 `TrailingStopPips + TrailingStepPips` 后才会生效，并按照固定步长平移，避免频繁调整。

## 参数
| 名称 | 类型 | 默认值 | MT5 对应参数 | 说明 |
| --- | --- | --- | --- | --- |
| `MaPeriod` | `int` | `15` | `Inp_MA_ma_period` | 趋势过滤移动平均的周期。 |
| `MaShift` | `int` | `5` | `Inp_MA_ma_shift` | 移动平均的向前偏移量。 |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | `Inp_MA_ma_method` | 平滑算法（SMA、EMA、SMMA、LWMA）。 |
| `PriceMode` | `CandlePrice` | `Weighted` | `Inp_MA_applied_price` | 参与计算的蜡烛价格类型。 |
| `StartVolume` | `decimal` | `1` | `InpStartLots` | 篮子第一笔订单的基础手数。 |
| `GridStepPips` | `decimal` | `30` | `InpStep` | 逆向加仓的步长（点）。 |
| `LotExponent` | `decimal` | `1.6` | `InpLotExponent` | 每次加仓的体积倍增系数。 |
| `LotMaximum` | `decimal` | `15` | `InpLotMaximum` | 单笔订单允许的最大手数。 |
| `ProfitMinimum` | `decimal` | `15` | `InpProfitMinimum` | 当篮子包含多单时要求的最小浮盈。 |
| `StopLossPips` | `decimal` | `0` | `InpStopLoss` | 固定止损距离（点，0 表示禁用）。 |
| `TakeProfitPips` | `decimal` | `100` | `InpTakeProfit` | 固定止盈距离（点）。 |
| `TrailingStopPips` | `decimal` | `15` | `InpTrailingStop` | 启动追踪止损所需的盈利。 |
| `TrailingStepPips` | `decimal` | `5` | `InpTrailingStep` | 每次移动追踪止损所需的额外盈利。 |
| `CandleType` | `DataType` | 15 分钟周期 | 图表周期 | 信号计算使用的时间框架。 |

## 与原始 EA 的差异
- StockSharp 采用净头寸模型，只维护单向净仓。策略内部记录每次开仓的价格和手数，以模拟 MT5 的篮子计算方式。
- 体积在下单前会根据交易所参数进行合法性校验与四舍五入，避免出现会被连接器拒绝的请求。
- 止损、止盈与追踪逻辑通过市价平仓实现，而非修改已有订单，但功能效果与 MT5 版本保持一致。

## 使用建议
- 请确保合约的 `PriceStep`、`StepPrice`、`MinVolume`、`VolumeStep`、`MaxVolume` 等信息完整，否则点值换算和体积
  取整可能不准确。
- 若交易品种的最小变动价位异常，需要相应调整 `GridStepPips`、`StopLossPips`、`TrailingStopPips` 等参数。
- 马丁格尔网格风险极高，建议先在历史数据上充分回测，并考虑真实的手续费与滑点，再投入真实交易。
