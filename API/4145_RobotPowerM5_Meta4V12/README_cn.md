# RobotPowerM5 Meta4 V12 策略

## 概述
RobotPowerM5 Meta4 V12 策略是 MetaTrader 4 顾问 `RobotPowerM5_meta4V12.mq4` 的 C# 移植版本。原始脚本运行在 5 分钟外汇
图表上，通过比较 Bulls Power 与 Bears Power 的强弱关系来决定是否建立新的多头或空头仓位。StockSharp 版本保持一次仅持有
一个仓位的设计，继续使用基于 MetaTrader “点（point）” 的止损/止盈设置，并重新实现了当行情朝有利方向发展时逐步锁定利润
的移动止损逻辑。

## 交易逻辑
1. **指标引擎**
   - 默认订阅 5 分钟 K 线（可通过 `CandleType` 参数修改时间框架）。
   - StockSharp 的 `BullsPower` 与 `BearsPower` 指标在每根收盘 K 线后更新，使用相同的周期参数。
   - 将两者之和 `BullsPower + BearsPower` 以一根 K 线的延迟进行保存，从而模拟 MQL 代码中的 `shift=1` 调用，即始终使用完全收
     盘的上一根 K 线数据。
2. **入场规则**
   - 当当前无持仓并且延迟后的 Bulls/Bears Power 之和为 **正值** 时，发送市价买单。
   - 当当前无持仓并且该数值为 **负值** 时，发送市价卖单。
   - 持仓期间忽略新的信号，仓位仅由保护性退出规则管理。
3. **仓位规模**
   - `Volume` 参数表示申请的手数，直接传递给 `BuyMarket` / `SellMarket`，由连接器按照合约的最小交易单位进行四舍五入。

## 风险控制
- **止损** – 初始止损距离入场均价 `StopLossPoints` 个 MetaTrader 点，通过多头的 K 线最低价或空头的最高价进行监控，只要价格
  触及即以市价离场。
- **止盈** – 止盈距离为 `TakeProfitPoints` 个点，使用 K 线极值判断是否触发，与 MT4 在单根 K 线内执行止盈的方式一致。
- **移动止损** – 当价格向有利方向突破 `TrailingStopPoints` 个点后启动移动止损。多头仓位的止损调整为
  `referencePrice - trailingDistance`，其中 `referencePrice` 取 K 线收盘价与最高价的较大值；空头仓位的止损调整为
  `referencePrice + trailingDistance`，其中 `referencePrice` 取收盘价与最低价的较小值。这一过程复刻了原脚本中通过
  `OrderModify` 实现的移动止损行为。

## 参数
| 名称 | 说明 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `BullBearPeriod` | Bulls/Bears Power 指标的平滑周期。 | `5` | 周期越大，动量滤波越平滑。 |
| `Volume` | 每次入场请求的手数。 | `1` | 实际成交量取决于品种的最小/最大手数及步长。 |
| `StopLossPoints` | 初始止损距离（点）。 | `45` | 设为 `0` 可关闭硬性止损。 |
| `TakeProfitPoints` | 止盈距离（点）。 | `150` | 设为 `0` 表示不使用固定止盈。 |
| `TrailingStopPoints` | 启动后的移动止损距离。 | `15` | 设为 `0` 可关闭移动止损。 |
| `CandleType` | 指标所使用的 K 线类型。 | `5 分钟` | 可选择任意其他 `DataType`。 |

## 实现细节
- 策略在内部保存所有风险控制水平（止损、止盈、移动止损），当 K 线确认价格穿越某个阈值时以市价离场，重现了 MT4 每个 tick
  调整订单的做法。
- 指标订阅通过 `Subscription.Bind` 完成，在单一回调中同时接收 Bulls Power 与 Bears Power 的最新值。
- 点值基于 `Security.PriceStep` 计算，因此参数可适配以 tick、pip 或分计价的各种品种。
- 入场判断始终使用上一根 K 线的指标值，避免在未收盘的 K 线上产生信号。

## 与 MQL 版本的差异
- 在 StockSharp 中采用显式的市价平仓而不是直接修改止损订单，对不同的连接器更加稳定，但总体逻辑一致。
- 借助 `StrategyParam` 对参数进行校验，防止出现诸如负数距离等无效配置。
- 利用 StockSharp 的高级 API 实现图表输出、指标绑定和数据订阅，无需手动遍历 tick 数据。
- MQL 脚本中的 EA 标识字符串在该移植版本中不再需要，因此被省略。
