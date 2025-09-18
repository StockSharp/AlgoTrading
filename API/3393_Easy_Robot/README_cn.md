# Easy Robot 策略

## 策略概述
Easy Robot 是一套顺势型交易策略，每当上一根小时线收盘后立即判断其方向：若收阳则在新柱开始时做多，若收阴则做空。策略一次只允许持有一个方向的仓位，忠实复刻原始的 MetaTrader 4 专家顾问。

## 交易规则
1. 订阅由 **CandleType** 参数指定的小时级别蜡烛（默认 H1）。
2. 在每根蜡烛结束时比较其收盘价与开盘价：
   - 收盘价高于开盘价：在当前无仓位时市价买入。
   - 收盘价低于开盘价：在当前空仓时市价卖出。
3. 下单数量使用策略的 `Volume` 属性，与 MQL 版本中通过 `CheckVolumeValue` 获得 0.01 手起步的逻辑一致。
4. 止损和止盈基于周期为 **AtrPeriod**（默认 14）的 **ATR** 指标：
   - 止损距离 = `ATR * StopFactor`。
   - 止盈距离 = `ATR * TakeFactor`。
   - 距离会根据最小报价步长/点值进行修正，确保保护单不会离价格过近。
5. 市价单成交后立即调用 `SetStopLoss` 与 `SetTakeProfit` 设置保护单，对应 MQL 里 `OrderSend` 的 `sl` / `tp` 参数。
6. 当 **UseTrailingStop** 为 true 时启用拖尾止损：当浮盈达到 **TrailingStartPips**（MetaTrader 点值）后，每当盈利创新高便按 **TrailingStepPips** 的间距上移/下移止损，同时保证与经纪商允许的最小距离相符。
7. 止损计算优先使用盘口最优买/卖价，若不存在则回退到最新成交价，再次退到蜡烛收盘价，等同于原始代码的 `Bid`/`Ask` 行为。

## 参数说明
| 名称 | 默认值 | 说明 |
|------|--------|------|
| `TakeFactor` | 4.2 | 止盈 ATR 倍数（对应 MQL 输入 `TakeFactor`）。|
| `StopFactor` | 4.9 | 止损 ATR 倍数（对应 `StopFactor`）。|
| `UseTrailingStop` | true | 是否启用拖尾止损（对应 `UseTstop`）。|
| `TrailingStartPips` | 40 | 启动拖尾所需盈利点数（对应 `Tstart`）。|
| `TrailingStepPips` | 19 | 拖尾每次移动的点数（对应 `Tstep`）。|
| `AtrPeriod` | 14 | ATR 指标周期。|
| `CandleType` | H1 | 用于信号与 ATR 计算的蜡烛周期。|

## 其他说明
- 当仓位回到零时会清空记录的入场价与止损价，以便下一次信号重新计算。
- 最小止损距离通过品种的点值（或价格步长）估算，对应原始包含文件中的 `SC` 函数。
- 启动时调用一次 `StartProtection()`，以便平台在需要时触发内置保护机制。
