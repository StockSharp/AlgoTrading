# Zone Recovery Area 策略

## 概述
**Zone Recovery Area Strategy** 是将 MetaTrader 专家顾问 “Zone Recovery Area”（`MQL/20266`）完整移植到 StockSharp 高级 API 的版本。策略保留了原有的区间恢复（zone recovery）对冲逻辑，并将主要参数全部公开，方便在不修改代码的情况下调优。开仓后，系统会围绕基准价格交替建立多、空头寸，当价格离开或重新进入预设区域时加仓，以期逐步弥补浮动亏损并最终以盈利关闭整个组合。

主要特性：
- 使用快、慢两条简单移动平均线（SMA）结合月度 MACD（12/26/9）作为趋势过滤器。
- 实现 zone recovery 对冲机制：第一笔交易确定基准价，后续对冲单在价格穿越区域边界或回到基准价时触发。
- 支持三种盈利退出方式：绝对金额、账户百分比、追踪止盈。
- 每一步加仓体量可按倍数递增（类马丁策略）或按固定增量增加。

## 数据与指标
- **主级别 K 线：** 用户自定义的入场与管理周期，默认 30 分钟。
- **月度 K 线：** 若无原生月线，可由更小周期合成，用于计算 MACD。
- **指标：**
  - 主周期上的两条 SMA。
  - 月度 MACD（含信号线）。

## 交易流程
1. **趋势确认**
   - 等待两条 SMA 与月度 MACD 均形成有效数值。
   - **多头条件：** 前一根 K 线中快 SMA 低于慢 SMA，且 MACD 主线高于信号线。
   - **空头条件：** 前一根 K 线中快 SMA 高于慢 SMA，且 MACD 主线低于信号线。
2. **启动恢复周期**
   - 出现多头（空头）信号时，以 `InitialVolume` 开多（空）头寸，并记录成交价为基准价。
   - 清空内部计数器与盈利跟踪变量，开始新的恢复周期。
3. **恢复引擎**
   - 计算两个关键价位：**恢复区间**（`ZoneRecoveryPips`）与**盈利目标**（`TakeProfitPips`）。
   - 周期运行过程中，每根完结的 K 线都需要检查：
     - 价格到达盈利目标时立即平掉所有净头寸，结束周期；
     - 若达到金额或百分比目标，或追踪止盈触发，也会立即平仓；
     - 否则判断是否需要新对冲单：
       - 多头周期：跌破 `base - zone` 时加空单，重新站回基准价时加多单；
       - 空头周期：突破 `base + zone` 时加多单，回落至基准价以下时加空单。
     - 多、空方向自动交替；每笔对冲单的手数根据设置自动放大或累加。
   - `MaxTrades` 控制单个周期内的最大交易次数。
4. **盈利管理**
   - `UseMoneyTakeProfit`：未实现利润达到设定金额时结束周期。
   - `UsePercentTakeProfit`：未实现利润达到账户价值的一定百分比时结束周期。
   - `EnableTrailing`：利润超过 `TrailingStartProfit` 后记录峰值，若回撤超过 `TrailingDrawdown` 即平仓。

策略使用 StockSharp 的高阶下单函数 `BuyMarket`/`SellMarket`，无需直接处理底层订单对象。

## 参数说明
| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `CandleType` | 30 分钟 | 入场与管理所用的主周期。 |
| `MonthlyCandleType` | 30 天 | 计算月度 MACD 的周期。 |
| `FastMaLength` | 20 | 快速 SMA 的周期。 |
| `SlowMaLength` | 200 | 慢速 SMA 的周期。 |
| `TakeProfitPips` | 150 | 从基准价起算的总体盈利目标。 |
| `ZoneRecoveryPips` | 50 | 恢复区间半宽度。 |
| `InitialVolume` | 1 | 周期第一笔订单的手数。 |
| `UseVolumeMultiplier` | true | 是否按倍数放大后续订单。 |
| `VolumeMultiplier` | 2 | 使用倍增模式时的乘数。 |
| `VolumeIncrement` | 0.5 | 使用加法模式时的增量。 |
| `MaxTrades` | 6 | 单个恢复周期内的最大订单数。 |
| `UseMoneyTakeProfit` | false | 启用金额止盈。 |
| `MoneyTakeProfit` | 40 | 金额止盈目标。 |
| `UsePercentTakeProfit` | false | 启用百分比止盈。 |
| `PercentTakeProfit` | 5 | 百分比止盈目标。 |
| `EnableTrailing` | true | 启用追踪止盈。 |
| `TrailingStartProfit` | 40 | 追踪止盈启动阈值。 |
| `TrailingDrawdown` | 10 | 允许的利润回吐。 |

> **点值转换：** `TakeProfitPips` 与 `ZoneRecoveryPips` 会根据标的的 `PriceStep` 转换为价格偏移，请确保证券提供正确的最小价位与步长价值。

## 使用建议
1. 在 Designer/API/Runner 中加载策略，并指定交易品种与投资组合。
2. 根据标的波动率与风险承受能力调整各项参数。
3. 确保历史数据长度足够，便于 SMA 与 MACD 完成预热。
4. 密切关注保证金占用，倍增模式下仓位会迅速扩大。
5. 先在回测或模拟环境验证，再考虑实盘运行。

## 风险提示
- 区间恢复/马丁策略在强趋势行情中可能累积巨额头寸，必须使用 `MaxTrades` 和合理的参数限制风险。
- StockSharp 采用净持仓模型，策略会根据 `PriceStep`/`StepPrice` 计算组合盈亏，建议与券商数据进行核对。
- 金额与百分比止盈依赖投资组合估值，回测时请确认 `BeginValue`、`CurrentValue` 等字段有效。
- 策略未设置硬止损，如有需要应在账户层面增加其他风控措施。

## 文件说明
- `CS/ZoneRecoveryAreaStrategy.cs` — 策略实现。
- `README.md` — 英文说明。
- `README_ru.md` — 俄文说明。
- `README_cn.md` — 中文说明（本文件）。

