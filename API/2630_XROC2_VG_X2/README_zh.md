# XROC2 VG X2 策略

## 概览
XROC2 VG X2 是一个多周期策略，通过两条平滑后的动量变化曲线来判断行情。较高周期用于确认方向性过滤，较低周期负责给出具体的入场与离场信号。原始的 MetaTrader 5 版本依赖自定义的 XROC2_VG 指标以及一套资金管理模块。移植到 StockSharp 后，策略保留了核心信号逻辑，并将主要参数暴露为策略输入。

策略会订阅两组 K 线：
- **高周期**（默认 6 小时）——用来判定当前的大趋势方向；
- **低周期**（默认 30 分钟）——通过观察两条平滑 ROC 曲线的交叉生成交易信号。

两条曲线共用同一种 ROC 计算模式，但拥有独立的平滑方式。默认情况下使用 Jurik 平滑，以贴近 MQL 实现。对于 StockSharp 尚未直接提供的平滑方式（JurX、ParMA、T3、VIDYA、带相位控制的 AMA），策略会退化到最接近的可用移动平均，因此这些组合下的表现可能与原版略有差异。

## 交易逻辑
1. **趋势识别（高周期）**
   - 按照设定的周期和平滑参数计算两条平滑 ROC。
   - 在 `HigherSignalBar` 指定的已收盘 K 线上比较两条曲线，快线在慢线上方视为多头趋势，反之为空头，若两者持平则保持趋势为零并暂停交易。
2. **信号生成（低周期）**
   - 在低周期上重复计算同样的两条平滑 ROC。
   - 取最近一个已收盘 K 线（偏移量 `LowerSignalBar`）及其前一根，根据两根柱子的相对位置判断是否刚发生交叉。
   - 当高周期为多头，且快线刚刚从上方向下穿越慢线并允许做多时，触发做多信号。
   - 当高周期为空头，且快线刚刚从下方向上穿越慢线并允许做空时，触发做空信号。
3. **头寸管理**
   - 低周期出现向下交叉或高周期趋势转为空头时（`CloseBuyOnLower`、`CloseBuyOnTrendFlip`），平掉多头。
   - 低周期出现向上交叉或高周期趋势转为多头时（`CloseSellOnLower`、`CloseSellOnTrendFlip`），平掉空头。
   - 仅在没有持仓时开新单，委托数量由策略的 `Volume` 属性控制。

## 参数说明
- `HigherCandleType`：趋势过滤所用的 K 线类型（默认 6 小时）。
- `LowerCandleType`：信号生成所用的 K 线类型（默认 30 分钟）。
- `HigherSignalBar`：读取高周期数据时的偏移量（单位：已收盘柱数，默认 1）。
- `LowerSignalBar`：读取低周期数据时的偏移量（默认 1）。
- `HigherRocMode` / `LowerRocMode`：ROC 计算方式（`Momentum`、`RateOfChange`、`RateOfChangePercent`、`RateOfChangeRatio`、`RateOfChangeRatioPercent`）。
- `HigherFastPeriod`、`HigherFastMethod`、`HigherFastLength`、`HigherFastPhase`：高周期快线的设置。
- `HigherSlowPeriod`、`HigherSlowMethod`、`HigherSlowLength`、`HigherSlowPhase`：高周期慢线的设置。
- `LowerFastPeriod`、`LowerFastMethod`、`LowerFastLength`、`LowerFastPhase`：低周期快线的设置。
- `LowerSlowPeriod`、`LowerSlowMethod`、`LowerSlowLength`、`LowerSlowPhase`：低周期慢线的设置。
- `AllowBuyOpen`、`AllowSellOpen`：是否允许开多/开空。
- `CloseBuyOnTrendFlip`、`CloseSellOnTrendFlip`：高周期趋势反转时是否立即平仓。
- `CloseBuyOnLower`、`CloseSellOnLower`：低周期交叉与持仓方向相反时是否平仓。

## 实现注意事项
- 原策略使用的平滑算法库非常庞大。移植版本将已支持的选项映射到 StockSharp 自带指标（SMA、EMA、SMMA/RMA、LWMA、Jurik、Kaufman AMA）。未支持的模式（JurX、ParMA、T3、VIDYA）自动退化为最接近的移动平均，因此某些组合下会与 MQL 结果略有出入。
- `TradeAlgorithms.mqh` 中的资金管理、止损止盈及滑点控制未被复现，策略仅按 `Volume` 固定手数下单。
- 订单均以市价成交，若需要保护性止损或跟踪止损，可通过 StockSharp 的保护模块自行添加。
- 只有在高低两个订阅都就绪并且 `IsFormedAndOnlineAndAllowTrading()` 返回 true 时才会触发交易逻辑。

## 使用建议
- 根据交易风格选择合适的周期组合（例如 6 小时 / 30 分钟适合波段交易），也可以尝试其他搭配。
- 调整 ROC 周期和平滑方式，以获得更契合的响应速度。若希望最大程度贴近原脚本，推荐保留 Jurik 平滑。
- 在真实账户运行时建议增加明确的风控措施（止损、仓位管理等），因为移植版本仅使用简单的市价平仓。
