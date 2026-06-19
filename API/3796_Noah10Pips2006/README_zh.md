# Noah 10 Pips 2006 策略

## 概述
- 重现 Noah10pips2006 MetaTrader 4 智能交易系统的区间突破与反转逻辑。
- 在时区偏移后的前一交易日高低点基础上构建通道，并在中点附近挂出突破止损单。
- 具备锁定利润后的跟踪止损、可选的风险控制下的动态手数以及一次性的反向加仓机制。

## 交易流程
1. **区间计算**  
   每个交易日开始时（考虑 `TimeZoneOffset` 设置），记录上一交易日的最高价与最低价，并据此计算：  
   - 中点价格。  
   - 向上/向下各 20 点（20 pips）的缓冲带。  
   - 若区间小于等于 160 点，则使用固定 40 点的入场带；否则使用区间的 25% 作为缓冲。
2. **首个挂单**  
   当市场进入交易窗口后：  
   - 若收盘价位于中点与上方缓冲之间，则在中点挂出卖出止损单。  
   - 若收盘价位于下方缓冲与中点之间，则在中点挂出买入止损单。  
   只有当入场带宽度大于 `MinimumRangePips` 时才会下单。
3. **补单机制**  
   如果只剩一个方向的挂单，策略会在对应缓冲价位补充另一方向的挂单，从而在两侧均准备突破头寸。
4. **持仓管理**  
   - 成交后立刻根据 `StopLossPips` / `TakeProfitPips` 建立保护性止损与止盈。  
   - 当浮动盈利达到 `TrailSecureProfitPips`，止损会被移动到锁定 `SecureProfitPips` 的位置。  
   - 若启用 `TrailingStopPips`，锁定盈利后会继续以该距离进行跟踪止损。
5. **日终清算**  
   当交易窗口结束，或周五达到 `FridayEndHour`，所有挂单及持仓都会被关闭。
6. **反向开仓**  
   首笔交易结束后，若未触发锁定盈利，策略会按原 EA 的逻辑以市价开启一次反向头寸。

## 重要参数
- `CandleType`：用于驱动计算的K线类型，默认 1 小时。  
- `TimeZoneOffset`：数据时间需要偏移的小时数。  
- `StartHour/StartMinute` 与 `EndHour/EndMinute`：交易窗口的开始与结束时间。  
- `FridayEndHour`、`TradeFriday`：周五的强制平仓时间与是否允许周五新开仓。  
- `StopLossPips`、`TakeProfitPips`：开仓后保护性止损/止盈的距离。  
- `SecureProfitPips`、`TrailSecureProfitPips`、`TrailingStopPips`：锁定盈利与后续跟踪的阈值与距离。  
- `MinimumRangePips`：入场通道的最小宽度。  
- `MinVolume/MaxVolume`、`MaximumRiskPercent`、`FixedVolume`：手数控制设置，与 MT4 版本中 `LotsRisk` 逻辑一致。

## 使用建议
- 若启用风险控制，合约必须提供有效的 `PriceStep` 与 `StepPrice`，否则请将 `FixedVolume` 设为 `true`。  
- 策略基于收盘价更新保护单，若需要更细粒度的管理可考虑缩短 K 线周期。  
- 固定 20/40 点缓冲假设为四位制外汇品种，若用于其他资产，请相应调整参数。  
- MT4 版本中的图形对象未被迁移，可在 StockSharp 图表中手动添加。
