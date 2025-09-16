# Force Trend 策略

## 概述
- 将 MetaTrader 5 顾问 **Exp_ForceTrend.mq5**（目录 `MQL/18817`）转换为 StockSharp 策略。
- 使用 ForceTrend 振荡指标来识别多空动量的切换。
- 基于 StockSharp 的高级 API 实现：订阅K线、绑定内置指标，而不是直接访问历史数组。

## ForceTrend 指标
- 在最近 `Length` 根K线中寻找最高价与最低价的区间。
- 将当前K线的中间价标准化到该区间内，并进行两次平滑：
  - 第一步通过系数 `0.66` 和 `0.67` 得到中间 `force` 值；
  - 第二步对 `force` 值进行对数变换并与前值做半衰期平滑，得到最终的 ForceTrend 数值。
- 数值大于零视为多头（原指标以蓝色显示），数值小于零视为空头（原指标以洋红色显示）。

## 参数
- `Length` —— ForceTrend 计算窗口长度，必须为正数。
- `SignalBar` —— 信号偏移的已完成K线数量。`0` 表示使用最新收盘K线，`1` 与 MT5 默认设置相同，等待一根额外K线，数值越大反应越慢。
- `EnableLongEntry` —— 是否允许在多头信号出现时开多。
- `EnableShortEntry` —— 是否允许在空头信号出现时开空。
- `EnableLongExit` —— 是否允许在空头信号出现时平多。
- `EnableShortExit` —— 是否允许在多头信号出现时平空。
- `CandleType` —— 用于计算指标的K线类型/周期。

## 交易规则
1. ForceTrend 数值被转换为离散方向（`+1`、`0`、`-1`）。
2. 使用固定长度的方向历史数组，对比 `SignalBar` 偏移位置与前一根的方向。
3. 当方向为多头 (`direction > 0`) 时：
   - 若 `EnableShortExit = true`，平掉所有空头仓位（数量为 `|Position|`）。
   - 若前一个方向不是多头且 `EnableLongEntry = true`，则以市价下单，数量为 `Volume + |Position|`，实现开多或反手。
4. 当方向为空头 (`direction < 0`) 时执行对称操作，依据 `EnableLongExit` 和 `EnableShortEntry` 控制。
5. 当指标为零时，沿用上一次有效方向，避免在零附近反复切换。
6. 只有在策略完全就绪且允许运行时（`IsFormedAndOnlineAndAllowTrading`）才会发送订单。

## 实现说明
- 通过 `SubscribeCandles(CandleType)` 订阅K线，`ProcessCandle` 回调中完成所有计算。
- 借助 `Highest` 与 `Lowest` 指标获取区间极值，无需手工维护列表或使用 LINQ。
- 方向历史存放在启动时预分配的固定数组中，以复现 MT5 的 `SignalBar` 行为并避免频繁分配。
- 反手交易只提交一张市价单，数量等于目标持仓与当前绝对持仓之和，对应 MQL 中的 `BuyPositionOpen` / `SellPositionOpen` 函数。
- 原顾问的资金管理、点数止盈止损和滑点参数未迁移；在 StockSharp 中通过 `Volume` 或外部保护模块自行控制风险。
- 布尔开关直接对应 MT5 输入参数（`BuyPosOpen`、`SellPosOpen`、`BuyPosClose`、`SellPosClose`）。

## 使用建议
- 启动前设置策略的 `Volume` 属性以控制下单数量。
- 选择与 MT5 测试相符的 `CandleType`（默认使用四小时K线）。
- 如需自动止损/止盈，可结合 StockSharp 的保护功能（例如 `StartProtection`）。

## 文件
- 策略实现：`CS/ForceTrendStrategy.cs`
- 原始 MQL 文件：`MQL/18817/mql5/Experts/Exp_ForceTrend.mq5`、`MQL/18817/mql5/Indicators/ForceTrend.mq5`
