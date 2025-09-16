# XD Range Switch 策略

## 概述
XD Range Switch 策略将 MetaTrader 5 顾问 **Exp_XD-RangeSwitch** 迁移到 StockSharp 高层 API。策略核心是自定义的 XD-RangeSwitch 通道指标，它会交替绘制上下包络线，并在主导方向切换时给出箭头提示。通过 `TradeDirection` 参数，策略既可以反向交易这些箭头（逆势模式），也可以顺势跟随通道突破。仓位规模由 `Strategy.Volume` 控制，原始 MT5 中的资金管理函数改由 StockSharp 的持仓管理逻辑完成。

## XD-RangeSwitch 指标重建
* 指标在最近 `Peaks` 根已完成 K 线内寻找最高价和最低价区间。
* 当当前收盘价高于前 `Peaks` 根 K 线的最高价时，绘制多头通道（下轨），其数值等于窗口内（含当前 K 线）的最低价。
* 当当前收盘价低于前 `Peaks` 根 K 线的最低价时，绘制空头通道（上轨），其数值等于同一窗口内（含当前 K 线）的最高价。
* 如果没有新的突破，通道取上一根 K 线的数值延续。
* 当某条通道在空缺后重新出现时，在通道价格位置生成箭头信号，对应 MT5 指标第 2、3 个缓冲区的写法。
* 仅处理收盘状态的 K 线，确保实时与历史回测时的数值一致。

## 交易逻辑
1. 策略订阅 `CandleType` 指定的时间框 K 线，并保存重建后的指标缓冲区。
2. 每根新 K 线到来时，读取 `SignalBar` 根之前的指标值，和 MT5 中 `CopyBuffer` 的偏移一致。
3. `TradeDirection` 控制信号解释方式：
   * **AgainstSignal**（默认）模拟 MT5 行为——上箭头触发做多并请求平掉空单，下箭头触发做空并请求平掉多单。
   * **WithSignal** 将解释反转：上箭头视为平多及做空信号，下箭头视为平空及做多信号，即顺势追随通道突破。
4. 即便没有箭头，只要通道仍存在也会触发平仓信号，对应原脚本中的 `SELL_Close` / `BUY_Close` 标志。
5. 平仓操作总是先于开仓执行，保证在翻向时先关闭反向仓位再建立新仓。
6. 交易通过 `BuyMarket` / `SellMarket` 市价助手完成。如果翻向时存在反向持仓，会自动叠加数量，先对冲旧仓再建立新仓。

## 风险管理
* `UseStopLoss`/`StopLossPoints` 与 `UseTakeProfit`/`TakeProfitPoints` 提供可选的止损、止盈功能。
* 距离以绝对价格单位表示，与 MT5 中的“点”一致。
* 每根收盘 K 线根据最高价/最低价检测是否触发止损或止盈，以模拟盘中触发。
* 当止损和止盈同时启用时，以先触发者为准，仓位一旦触及任意目标即被平掉。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | H4 K 线 | 计算 XD-RangeSwitch 指标的时间框。 |
| `Peaks` | 4 | 指标分析的极值数量（窗口长度）。 |
| `SignalBar` | 1 | 读取指标缓冲区时回看的已完成 K 线数量。 |
| `TradeDirection` | AgainstSignal | 选择逆势或顺势的信号解释方式。 |
| `AllowBuyEntry` / `AllowSellEntry` | true | 是否允许开多 / 开空。 |
| `AllowBuyExit` / `AllowSellExit` | true | 是否允许按指标信号平多 / 平空。 |
| `UseStopLoss` / `StopLossPoints` | true / 1000 | 是否启用止损以及其距离（价格单位）。 |
| `UseTakeProfit` / `TakeProfitPoints` | true / 2000 | 是否启用止盈以及其距离（价格单位）。 |

## 备注
* 为遵循转换规范，最高价/最低价缓冲区在策略内部维护，不依赖额外集合，从而完全还原 MT5 逻辑。
* 只在 K 线收盘后评估信号；当 `SignalBar > 0` 时，订单会在下一根 K 线执行，与 MT5 顾问保持一致。
* 指标历史缓存长度根据 `Peaks` 与 `SignalBar` 的最大值动态裁剪并保留小幅余量，长时间回测时内存占用保持稳定。
* 默认配置与 MT5 相同：H4、`Peaks = 4`、`SignalBar = 1`、逆势模式以及 1000/2000 点的风险区间。
