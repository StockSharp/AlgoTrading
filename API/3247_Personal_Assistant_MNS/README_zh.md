# Personal Assistant MNS 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 中的 `personal_assistant_codeBase_MNS` 助手 EA 迁移到 StockSharp。它是一个人工辅助模块：不会主动生成信号，而是通过公开方法模拟原版热键（开/平仓、调整手数、在盈利时强制平仓）。此外，每根完结 K 线都会在日志中输出关于合约、活动委托和风险设置的详细信息。

## 工作流程

1. 订阅参数 `CandleType` 指定的 K 线（默认 1 分钟）。
2. 当 K 线收盘时，日志会记录当前仓位、浮动盈亏、处于激活状态的止损/止盈委托数量、点差、每跳价值以及策略的 magic number。
3. `PressBuy()`、`PressSell()` 等方法会按照当前助手手数下达市价单；若设置了 `TakeProfitPips` 与 `StopLossPips`，则把点数转换为绝对价格并缓存。
4. 缓存的保护位通过 K 线高低价来模拟触发，一旦达到水平就使用市价单平仓。
5. `UseTrailingStop` 为真时启用“移动到保本”规则：当浮盈达到 `BreakEvenTriggerPips` 时激活，之后若价格回撤到入场价加上 `BreakEvenOffsetPips`，策略立即平仓。

## 功能亮点

- 公开的方法完全覆盖原版按钮 1–8：
  - `PressBuy()` / `PressSell()`——开多/开空，并缓存可选的止盈止损。
  - `PressCloseAll()`——一键平掉全部仓位。
  - `IncreaseVolume()` / `DecreaseVolume()`——以 0.01 手为步长调节交易量。
  - `CloseLongPositions()` / `CloseShortPositions()`——只平掉单边仓位。
  - `CloseProfitablePositions()`——浮盈为正时强制平仓。
- `DisplayLegend` 打开时在启动阶段输出详细的操作提示。
- 使用合约的 `PriceStep`、`StepPrice` 和 `Decimals` 将点数转换为价格距离。
- 复刻原 EA 的 `MOVETOBREAKEVEN()`，对多头和空头均支持保本移动。
- 为多空分别缓存止盈止损，切换方向时自动清理过期水平。

## 参数说明

| 参数 | 说明 |
|------|------|
| `MagicNumber` | 与 MQL 输入 `MagicNo` 对应的标识符，仅用于日志。 |
| `DisplayLegend` | 是否在每根 K 线收盘时输出操作提示和状态信息。 |
| `OrderVolume` | 所有手动操作共享的基础交易量（手）。 |
| `Slippage` | 允许的最大滑点（跳数），作为参考信息存储。 |
| `TakeProfitPips` | 止盈距离（点），设为 0 表示禁用。 |
| `StopLossPips` | 止损距离（点），设为 0 表示禁用。 |
| `UseTrailingStop` | 是否启用保本移动。 |
| `BreakEvenTriggerPips` | 激活保本移动所需的盈利（点）。 |
| `BreakEvenOffsetPips` | 保本时在入场价基础上加减的偏移量（点）。 |
| `CandleType` | 用于监控和模拟保护位的 K 线类型。 |

## 使用建议

- 可在 Designer、脚本或自定义 UI 中调用这些方法，以模拟 MetaTrader 中的热键操作。
- 保护位及保本逻辑依赖合约提供 `PriceStep`、`StepPrice` 与 `Decimals`。若数据缺失，可手动调整点数，或把相关参数设为 0 以关闭功能。
- 因为通过 K 线高低价判断触发，若希望捕捉更细颗粒的波动，请缩短 `CandleType` 的周期。
- `CloseProfitablePositions()` 等同于原版按钮 8：只有当浮动盈亏大于 0 时才会平仓。

## 与 MetaTrader 版本的差异

- StockSharp 策略无法像 EA 那样在图表上创建文本和按钮，因此改为在日志中输出提示。
- 止损/止盈不再以挂单形式提交，而是在条件满足时直接通过市价单退出。
- 保本移动使用市价单实现，不会修改已存在的保护委托。
- `Slippage` 仅保留为参考，实际成交由连接器控制。
