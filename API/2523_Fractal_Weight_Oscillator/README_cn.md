# 分形权重振荡器策略

## 概述
该策略是对 "Exp_Fractal_WeightOscillator" 专家的移植，通过组合四个
振荡器（RSI、资金流量指数、威廉指标 %R 和 DeMarker）生成一个平滑的
综合信号。策略在所选时间框架上计算指标，并将综合振荡器与
`HighLevel` / `LowLevel` 两条水平线比较，在趋势跟随或逆趋势模式下
触发多空交易。所有逻辑均使用 StockSharp 的高级 API 实现。

## 指标结构
- **RSI**：应用于配置的价格源。
- **MFI**：基于所选价格和K线成交量计算。
- **Williams %R**：使用K线高/低/收价。
- **DeMarker**：根据高低价重建，并采用简单的移动平均平滑。
- **平滑移动平均**：可选的后处理器（SMA、EMA、SMMA 或 LWMA）。

综合振荡器是上述四个指标的加权平均值。`HighLevel` 与
`LowLevel` 定义超买/超卖区域；`SignalBar` 决定在信号判断时回溯的
已完成K线数量，用于控制信号延迟。

## 交易逻辑
### TrendMode = Direct（顺势）
- **做多 / 平空**：当振荡器从 `LowLevel` 之上跌至 `LowLevel`
  （需要 `BuyOpenEnabled` 和 `SellCloseEnabled` 为 true）。
- **做空 / 平多**：当振荡器从 `HighLevel` 之下升至 `HighLevel`
  （需要 `SellOpenEnabled` 和 `BuyCloseEnabled` 为 true）。

### TrendMode = Counter（逆势）
- **做多 / 平空**：当振荡器向上突破 `HighLevel`。
- **做空 / 平多**：当振荡器向下跌破 `LowLevel`。

信号在 `SignalBar` 指定的已完成K线上评估。反向开仓时使用
`Volume + |Position|`，以先平掉旧仓位。

## 风险管理
开仓后会根据 `StopLossPoints` 与 `TakeProfitPoints` 计算固定价位的
止损/止盈（以 `MinPriceStep` 为单位）。每根完成的K线都会检查高低价
是否触及这些价位，一旦命中立即平仓并重置风险参数。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `TrendMode` | 选择顺势或逆势模式。 |
| `SignalBar` | 评估信号时回溯的已完成K线数量。 |
| `Period` | RSI、MFI、Williams %R、DeMarker 的基础周期。 |
| `SmoothingLength` | 平滑移动平均的窗口长度。 |
| `SmoothingMethod` | 移动平均类型（`None`、`Sma`、`Ema`、`Smma`、`Lwma`）。 |
| `RsiPrice`、`MfiPrice` | 组成指标使用的价格源。 |
| `MfiVolume` | MFI 的成交量类型（Tick 与 Real 都使用K线总量）。 |
| `RsiWeight`、`MfiWeight`、`WprWeight`、`DeMarkerWeight` | 各子指标的权重。 |
| `HighLevel`、`LowLevel` | 振荡器的上/下阈值。 |
| `BuyOpenEnabled`、`SellOpenEnabled` | 是否允许开多/开空。 |
| `BuyCloseEnabled`、`SellCloseEnabled` | 是否允许在反向信号时平仓。 |
| `StopLossPoints`、`TakeProfitPoints` | 止损与止盈（单位为价格步长，0 表示禁用）。 |
| `CandleType` | 计算所用K线的类型/周期。 |
| `Volume` *(策略属性)* | 开仓手数；反手时自动加上当前仓位绝对值。 |

## 使用说明
- `SignalBar = 1` 重现原始EA的行为，使用最新完成的K线进行判断。
  增加该值可以让策略在更旧的K线上确认信号。
- `SmoothingMethod` 可关闭平滑（`None`），或选择不同的移动平均方式
  以贴近MQL版本。
- MFI 计算始终使用数据源提供的K线总成交量。因为 StockSharp 标准
  K线默认不区分逐笔数量，`Tick` 与 `Real` 两个选项使用相同数据。
- 源码中的注释全部为英文，以符合仓库要求。
