# True Scalper Profit Lock 策略

## 概述
**True Scalper Profit Lock 策略** 是 MetaTrader 5 智能交易系统 “True Scalper Profit Lock” 的 StockSharp 移植版本。策略以超短线交易为目标，结合快速指数移动平均线、2 周期 RSI 过滤器以及将止损推向保本位置的利润保护机制。当持仓在设定的 K 线数量内没有达到目标时，“弃单” 模块会强制离场。

实现代码仅订阅一个 K 线数据流，并且只在 K 线收盘后做出决策。默认适用于日内剥头皮场景，但所有参数都可以调整，从而适配不同品种与周期。

## 指标与数据
- **EMA（快线）** – 默认周期为 3，用于识别向上的动量。
- **EMA（慢线）** – 默认周期为 7，定义短期趋势方向。
- **RSI** – 默认周期为 2，提供两种判定模式：
  - *Method A*（默认关闭）：仅在 RSI 穿越阈值时改变信号方向。
  - *Method B*（默认开启）：根据 RSI 位于阈值之上或之下决定信号方向。
- **K 线** – 默认时间框架为 1 分钟，可通过 `CandleType` 参数配置。

## 入场逻辑
1. 在最新收盘 K 线上计算快慢 EMA 与 RSI。
2. 根据所选模式确定 RSI 极性：
   - Method A：比较当前值与上一根 K 线的值，识别阈值穿越。
   - Method B：直接判断当前 RSI 是否高于或低于阈值。
3. **做多条件** – 当快 EMA 至少高于慢 EMA 一个最小报价步长，并且 RSI 极性为 “负” 时入场；若弃单逻辑要求反向做多，也会忽略当前信号直接建仓。
4. **做空条件** – 当快 EMA 至少低于慢 EMA 一个最小报价步长，并且 RSI 极性为 “正” 时入场；弃单反向信号同样会触发做空。
5. 反手时，使用一笔市场单在同一时间平掉旧仓并建立新仓。

## 离场逻辑
- **止损 / 止盈** – 以 `StopLossPoints`、`TakeProfitPoints` 指定的价格步长距离在入场后立即生效。
- **利润锁定** – 启用时，持仓盈利达到 `BreakEvenTriggerPoints` 指定的步长后，将止损移动到入场价再加上 `BreakEvenPoints` 的安全垫（做空时为入场价减去该距离）。每笔交易只执行一次。
- **弃单逻辑** – 根据入场后经历的收盘 K 线数量触发：
  - *Method A*：达到 `AbandonBars` 后平仓，并在下一次机会强制反向建仓。
  - *Method B*：达到阈值后仅平仓，后续方向继续依赖信号。
  - 当两种方法同时开启时，Method A 拥有优先级。
- 所有离场均使用市场单（`ClosePosition`）完成，并在执行后重置内部状态。

## 资金管理
- 启用 `UseMoneyManagement` 时，按照原始 EA 的公式动态计算手数：`Ceiling(Balance * RiskPercent / 10000) / 10`。
- 计算结果会遵守 MT5 版的边界条件：低于 0.1 手时回落到 `InitialVolume`，大于 1 手时向上取整，迷你账户可乘以 10，总手数上限为 100。
- 关闭资金管理时始终使用固定的 `InitialVolume`。

## 参数说明
- `InitialVolume` – 关闭资金管理时的基础手数。
- `TakeProfitPoints` / `StopLossPoints` – 以 `Security.PriceStep` 表示的止盈、止损距离。
- `FastPeriod`、`SlowPeriod`、`RsiLength`、`RsiThreshold` – 指标配置。
- `UseRsiMethodA`、`UseRsiMethodB` – 选择 RSI 判定逻辑。
- `UseAbandonMethodA`、`UseAbandonMethodB`、`AbandonBars` – 弃单模块设置。
- `UseMoneyManagement`、`RiskPercent`、`LiveTrading`、`IsMiniAccount` – 资金管理相关选项，与原 EA 保持一致。
- `UseProfitLock`、`BreakEvenTriggerPoints`、`BreakEvenPoints` – 保本移动参数。
- `MaxPositions` – 为兼容 MQL 版本保留。当前移植采用净仓制度，仍然一次只管理一个净头寸。
- `CandleType` – 信号所使用的周期或自定义 K 线类型。

## 使用提示
- 策略只需绑定一个交易品种，`GetWorkingSecurities` 会自动订阅所选的 K 线类型。
- 利润锁定与弃单逻辑依赖收盘价，因此同一根 K 线内的瞬时穿越不会触发。
- 原 MT5 参数 `Slippage` 在源码中未被使用，因此移植版本未包含该设置。
- 请根据标的的最小报价步长调节相关参数，以维持原本的点差距离。

## 转换差异
- StockSharp 采用净仓模式，即便 `MaxPositions` 大于 1 也不会同时开启多笔同向持仓，这与原 EA 在 `maxTradesPerPair = 1` 时的表现一致。
- 订单管理改用 `BuyMarket`、`SellMarket`、`ClosePosition` 等高阶 API，不再直接操作交易票据。
- 指标数据通过 `Bind` 回调传入，无需手动访问指标缓冲区。

## 测试建议
- 在与原 EA 相同的时间框架（推荐 1 分钟）上进行历史回测，验证行为是否一致。
- 针对目标品种优化 `TakeProfitPoints`、`StopLossPoints` 与 `BreakEvenTriggerPoints`，因为原始数值针对外汇点值设定。
