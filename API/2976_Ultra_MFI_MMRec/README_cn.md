# Ultra MFI 资金管理重算策略

## 概述
**Ultra MFI MMRec 策略** 直接移植自 MetaTrader 5 专家顾问 `Exp_UltraMFI_MMRec`。策略将多级平滑的资金流量指数（MFI）与基于连败的仓位管理结合在一起。两个内部计数器统计有多少平滑层指向上方或下方。当计数器发生交叉时产生交易信号，而最近几笔交易的盈亏则决定下一笔订单使用常规仓位还是缩减仓位。

## 交易逻辑
1. **基础指标**：在所选 K 线类型上计算具有可配置周期的资金流量指数。
2. **阶梯平滑**：将 MFI 数值依次送入一组平滑平均线。每一层都会把长度增加固定的步长。目前支持的平滑方法包括简单、指数、平滑、线性加权以及 Jurik 移动平均（MT5 中的 JurX、Parabolic、VIDYA、AMA 等模式在 StockSharp 中不可用）。
3. **方向计数器**：每根 K 线都会比较每个平滑层当前与上一根的输出。如果该层正在上升，则增加多头计数器，否则增加空头计数器。随后再使用终端平滑器对这两个计数器进行平滑。
4. **信号位移**：策略只处理收盘 K 线。`SignalShift` 参数控制回看多少根已完成的 K 线，与 MT5 中 `SignalBar=1` 的行为一致。
5. **开平仓规则**：
   * 如果上一根 K 线显示多头更强（`bulls > bears`），而最近一根 K 线出现交叉变为 `bulls < bears`，策略将在空仓时开多，同时在持有空头时先行平仓。
   * 如果上一根 K 线显示空头更强，最近一根翻转为 `bulls > bears`，则在空仓时开空，同时在持有多头时先行平仓。
   * 可选的百分比止盈止损可通过 `StartProtection` 启用。
6. **仓位管理**：每次平仓后都会检查方向性盈亏，以决定下一笔订单的手数。
   * 策略保存最近 `BuyTotalTrigger` 笔多头交易，并统计其中的亏损次数。亏损数达到 `BuyLossTrigger` 时，下一笔多头使用 `ReducedVolume`，否则使用 `NormalVolume`。
   * 空头部分独立使用 `SellTotalTrigger` 和 `SellLossTrigger` 进行同样的判断。

## 参数
- **CandleType**：用于生成信号的 K 线类型/时间框。
- **MfiPeriod**：资金流量指数的周期。
- **StepSmoothing / FinalSmoothing**：阶梯平滑和终端计数器的移动平均类型。
- **StartLength / StepSize / StepsTotal**：阶梯平滑的初始长度、步长以及层数。
- **FinalSmoothingLength**：终端计数器的平滑长度。
- **SignalShift**：计算信号时回看的已完成 K 线数量。
- **NormalVolume / ReducedVolume**：正常仓位与缩减仓位的手数。
- **BuyTotalTrigger / BuyLossTrigger**：统计多头交易时使用的历史深度与连亏阈值。
- **SellTotalTrigger / SellLossTrigger**：空头交易的历史深度与连亏阈值。
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits**：分别启用或禁用多空方向的开仓与平仓。
- **TakeProfitPercent / StopLossPercent**：可选的百分比止盈止损。

## 使用提示
- 阶梯平滑需要足够的历史数据才能稳定运行，请在指标完全形成后再开始评估信号。
- 由于 StockSharp 中缺少 MT5 的部分平滑算法，策略提供的选项为可用的最接近替代方案，推荐默认使用 Jurik 平滑以保持指标风格。
- 仓位管理依赖于真实的已实现盈亏，因此回测时需要让订单执行完成，以便在每次平仓后更新盈亏。
- 策略会在持仓平掉之后再尝试反向开仓。如果在持有空头时出现做多信号，系统会先平掉空头，并在下一根合适的 K 线重新评估做多条件。
