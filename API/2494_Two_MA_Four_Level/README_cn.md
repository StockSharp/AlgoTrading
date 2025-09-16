# 双均线四级策略
[English](README.md) | [Русский](README_ru.md)

该策略等价于 MetaTrader 顾问 "2MA_4Level"，使用 StockSharp 的高级 API 实现。策略基于两条平滑移动平均线（SMMA），以每根K线的中价 `(High + Low) / 2` 作为输入。系统不仅监控快慢均线的直接交叉，还检测上下两个区间的四个偏移阈值。只有在没有持仓时才会开仓，每笔交易都绑定固定点数的止损和止盈。

## 交易逻辑

- 对所选时间框架的K线计算快线和慢线SMMA（默认周期分别为50和130）。
- 在每根已完成的K线上比较当前与上一根K线的SMMA数值，确认交叉方向。
- 交叉判断包含五条基准线：
  1. 原始慢线；
  2. 慢线 + `MostTopLevel` 点；
  3. 慢线 + `TopLevel` 点；
  4. 慢线 − `LowermostLevel` 点；
  5. 慢线 − `LowerLevel` 点。
- 当快线向上穿越任一基准线时（且当前为空仓）开多单；当快线向下穿越任一基准线时开空单。
- 通过 `StartProtection` 函数，利用合约的最小价位变动 (`Security.PriceStep`) 自动附加止损和止盈。

策略不会加仓或反手，必须等待上一笔仓位被止盈或止损后才能开新单。

## 参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `FastPeriod` | 50 | 快速SMMA的周期，必须小于 `SlowPeriod`。|
| `SlowPeriod` | 130 | 慢速SMMA的周期。|
| `MostTopLevel` | 500 | 最高的上方偏移（点数），用于最宽松的确认条件，必须大于 `TopLevel`。|
| `TopLevel` | 250 | 第二级上方偏移（点数）。|
| `LowerLevel` | 250 | 第二级下方偏移（点数），必须小于 `LowermostLevel`。|
| `LowermostLevel` | 500 | 最低的下方偏移（点数）。|
| `TakeProfitPips` | 55 | 止盈距离，单位为点。|
| `StopLossPips` | 260 | 止损距离，单位为点。|
| `CandleType` | 15 分钟 | 用于指标计算和信号生成的K线类型。|

## 实现细节

- 使用中价作为指标输入，以匹配 MT5 中的 `PRICE_MEDIAN` 设置。
- 仅对已收盘的K线进行运算和判断，避免了未完成K线带来的噪音。
- `StartProtection` 在启动时调用一次，此后每笔委托都会继承统一的止损/止盈距离。
- `OnStarted` 中包含参数合法性检查（如 `FastPeriod >= SlowPeriod`），若配置不正确会写入错误日志并立即停止策略。

## 使用建议

1. 绑定的证券应当提供有效的 `PriceStep`，否则点值会回退为 `1`，可能导致风险控制不准确。
2. 原版策略要求 MT5 对冲账户；在 StockSharp 中同样只允许一个净头寸，避免出现同时持有多笔订单的情况。
3. `FastPeriod` 与 `SlowPeriod` 已启用优化标记，可直接使用 StockSharp 优化器进行参数寻优。
4. 策略仅依赖止损和止盈退出，请根据标的波动性调整点差，防止过长的持仓时间或过早止损。

## 文件列表

- `CS/TwoMaFourLevelStrategy.cs` —— 策略 C# 实现。
- `README.md` —— 英文说明。
- `README_ru.md` —— 俄文说明。
