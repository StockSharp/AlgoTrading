# 鳄鱼指标分形马丁策略

该策略把 MetaTrader 上的 “Alligator(barabashkakvn's edition)” 专家顾问移植到 StockSharp 的高级 API。策略结合了比尔·威廉姆斯的鳄鱼指标、分形突破确认、可选的马丁格尔加仓梯队以及自适应追踪止损。首笔单以市价开仓，当行情不利时按照预先设定的价格间距逐级增加头寸。

## 交易逻辑

- **鳄鱼张口**：使用中价驱动鳄鱼指标的唇线（绿）、齿线（红）、颚线（蓝）三条平滑移动平均。唇线高出颚线至少 `EntrySpread` 时激活做多偏向，反向条件激活做空偏向；当价差收窄至 `ExitSpread` 以下则关闭对应偏向。
- **分形过滤（可选）**：在每根已完成的 K 线上寻找比尔·威廉姆斯分形。做多必须在最近 `FractalLookback` 根内存在一个至少高于收盘价 `FractalBuffer` 的上分形；做空则要求存在下分形。若关闭 `UseFractalFilter`，策略只根据鳄鱼信号入场。
- **马丁格尔加仓**：首笔成交后，可预先生成 `MartingaleSteps` 个均价层，每层间距为 `MartingaleStepDistance`，体量按照 `MartingaleMultiplier` 逐级放大并受 `MaxVolume` 限制。一旦价格触碰相应层位便立即执行加仓。
- **追踪退出管理**：每个持仓都会根据 `StopLossDistance` 和 `TakeProfitDistance` 赋予合成止损与止盈。若启用 `EnableTrailing`，止损会在价格向有利方向运行并超过 `TrailingStep` 后自动上移（或下移）。
- **鳄鱼离场（可选）**：当 `UseAlligatorExit` 为真且鳄鱼重新闭口时，策略立即平掉对应方向的持仓。

## 风险与订单处理

- `Volume` 参数决定首笔市价单的数量。马丁格尔梯队会在此基础上进行步进放大，并对结果进行最小交易单位与 `MaxVolume` 限制的四舍五入。
- 止损、止盈完全在策略内部按收盘 K 线计算，而不是依赖交易所原生委托；当 K 线范围触及合成价位时立即平仓。
- 在开出反向仓位前会先行平掉现有仓位，避免在 StockSharp 中形成对冲敞口。

## 参数说明

| 参数 | 说明 |
| --- | --- |
| `Volume` | 首笔市价单数量。 |
| `JawLength`、`TeethLength`、`LipsLength` | 构成鳄鱼指标颚、齿、唇的平滑移动平均长度。 |
| `JawShift`、`TeethShift`、`LipsShift` | 读取鳄鱼指标缓冲区时使用的前移位数。 |
| `EntrySpread`、`ExitSpread` | 激活与关闭交易信号所需的价差阈值。 |
| `UseAlligatorEntry`、`UseAlligatorExit` | 控制是否使用鳄鱼指标进行入场/离场。 |
| `UseFractalFilter` | 是否启用分形突破过滤层。 |
| `FractalLookback`、`FractalBuffer` | 分形有效期与距离过滤参数。 |
| `EnableMartingale`、`MartingaleSteps`、`MartingaleMultiplier`、`MartingaleStepDistance`、`MaxVolume` | 控制马丁格尔加仓梯队。 |
| `StopLossDistance`、`TakeProfitDistance`、`EnableTrailing`、`TrailingStep` | 配置合成止损、止盈与追踪逻辑。 |
| `AllowMultipleEntries` | 允许在已有仓位的情况下重复市价入场。 |
| `ManualMode` | 手动模式，仅管理持仓不再新开仓。 |
| `CandleType` | 用于计算的 K 线类型。 |

## 使用建议

1. 确保交易品种的最小价格步长与数量步长支持所设置的距离，策略会在可用时依据 `Security.MinPriceStep` 与 `Security.VolumeStep` 进行对齐。
2. 马丁格尔梯队在策略内部模拟执行，若希望使用真实的限价委托，请关闭此功能并自行管理加仓。
3. 建议在允许对冲的投资组合中运行；尽管 StockSharp 聚合净仓位，原版逻辑假定可以在同向逐级加仓。
4. 默认的距离基于外汇常见点值（例如 `0.008`≈80 点），在用于其他品种前请根据实际价格尺度调整。 
