# Little EA 策略

## 概述
Little EA 最初是为 MetaTrader 编写的移动均线交叉专家。策略会根据 **OHLC bar index** 参数选择特定的历史 K 线，并在该 K 线向上或向下穿越位移后的移动均线时做出反应。StockSharp 版本保留了原始系统的分批入场思想，通过对每个方向设置可配置的最大敞口来允许多笔仓位。

## 交易逻辑
1. 订阅所选周期的蜡烛数据，并使用指定的价格来源（收盘价、开盘价、最高价、最低价、中值价、典型价或加权价）驱动对应类型的移动均线。
2. 保存已经收盘的蜡烛，以便根据 `OhlcBarIndex` 参数访问目标 K 线（默认值 `1` 表示上一根完全收盘的蜡烛）。
3. 通过 `MaShift` 读取若干根之前的移动均线数值，用来模拟 MetaTrader 中的视觉位移效果。
4. 当参考蜡烛的收盘价高于位移后的移动均线时视为看多交叉；收盘价低于位移后的移动均线时视为看空交叉。
5. 看多交叉时：
   - 如果当前空头净敞口已经达到上限，则立即平掉全部空头仓位。
   - 否则在多头敞口未触及上限时，按 `TradeVolume` 增加一笔多头仓位。
6. 看空交叉时：
   - 如果当前多头净敞口已经达到上限，则立即平掉全部多头仓位。
   - 否则在空头敞口未触及上限时，按 `TradeVolume` 增加一笔空头仓位。

这样的敞口限制可以在 StockSharp 的净头寸模式下模拟原 EA 中 `Int_Max_Pos` 对累计仓位数量的控制。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟 | 用于计算信号和指标的主要周期。 |
| `OhlcBarIndex` | `int` | `1` | 用于判断交叉的历史蜡烛索引（0 = 当前正在形成的蜡烛，1 = 最近收盘的蜡烛）。 |
| `MaxPositionsPerSide` | `int` | `15` | 每个方向允许累计的 `TradeVolume` 批次数上限。 |
| `MaPeriod` | `int` | `64` | 移动均线的计算长度。 |
| `MaShift` | `int` | `0` | 在检测交叉时向后引用移动均线的蜡烛数量。 |
| `MaType` | `MovingAverageType` | `Smoothed` | 移动均线的计算方式（Simple、Exponential、Smoothed、Weighted）。 |
| `AppliedPrice` | `AppliedPriceType` | `Close` | 指标输入所使用的价格。 |
| `TradeVolume` | `decimal` | `1` | 每次开仓或加仓时使用的交易数量。 |

## 与原始 MetaTrader 专家顾问的差异
- 资金管理被简化为固定交易量，不再支持原 EA 中的百分比风险模型。
- StockSharp 采用净头寸模式，因此在切换方向之前会先平掉反向持仓，并按照净敞口（而非独立订单数量）执行 `MaxPositionsPerSide` 的限制。
- 指标计算与数据缓存通过高级烛图订阅 API 完成，无需手动复制缓冲区数据。

## 使用建议
- 启动策略前请将 `TradeVolume` 调整到合约支持的最小交易步长；构造函数同时将该值赋给 `Strategy.Volume`，便于调用标准下单方法。
- 如果需要还原 MetaTrader 中的视觉效果，可结合使用 `MaShift` 与 `OhlcBarIndex` 调整参考蜡烛和指标偏移。
- 建议将策略附加到图表，以同时查看蜡烛、移动均线及成交记录，从而验证交叉信号。

## 指标
- 一条可配置的移动均线（Simple、Exponential、Smoothed 或 Weighted）。
