# SwingTrader 策略

## 概述
**SwingTrader 策略** 是对 MetaTrader 4 专家顾问 `SwingTrader.mq4` 的 StockSharp 版本移植。原始 EA 通过观察价格在
布林带外轨附近的“触碰-回撤”行为来入场：当价格触及上/下轨后，下一根 K 线突破中轨时开仓，然后使用倍数加仓的
网格进行摊薄。移植后的策略使用 StockSharp 的 K 线订阅、`StockSharp.Algo.Indicators` 中的布林带指标以及
`BuyMarket`/`SellMarket` 辅助函数重建同样的逻辑，同时遵守交易所提供的 `Security` 元数据约束。

## 交易逻辑
1. 订阅参数 `CandleType` 指定的周期，创建长度为 `BollingerPeriod`、标准差倍数固定为 2 的布林带。
2. 只处理已经收盘的 K 线，仿照 MT4 中 `IsNewCandle()` 的做法忽略未完成的柱。
3. 跟踪上一根柱是否触及上轨或下轨。布尔变量 `_upTouch` / `_downTouch` 完全复刻 MT4 中的互斥切换逻辑，确保
   在出现反向触碰之前只保留一个有效方向。
4. 当没有网格持仓时：
   - 如果最近收盘的柱在触及下轨后向上穿越中轨，则按 `InitialVolume`（经过交易所最小变动调整后的值）买入；
   - 如果最近收盘的柱在触及上轨后向下穿越中轨，则按 `InitialVolume` 卖出。
   首单成交价被记录为锚定价格，网格间距等于当时布林带的上下轨差值。
5. 当已经存在网格时，监控价格相对于锚定价的不利波动：
   - 多头：若当前柱的最低价比锚定价低至少一个网格宽度，则按几何序列（乘以 `Multiplier`）再买入一单；
   - 空头：若当前柱的最高价比锚定价高至少一个网格宽度，则按相同倍数再卖出一单。
6. 持续加仓，直到浮动盈亏达到目标或触发最大允许亏损。

## 资金管理与离场
- `CalculateUnrealizedProfit` 将价格差转换为 `Security.PriceStep` 与 `Security.StepPrice` 描述的最小价位和 Tick 价值，
  从而复现 MT4 里的浮动盈亏计算方法。
- 投入资金的估算沿用原公式 `Lots * Price / TickSize * TickValue / 30`：`Lots` 为所有网格仓位的体积之和，`TickSize`、
  `TickValue` 对应 `Security` 中的步长和 tick 价值。
- 当浮动利润超过 `TakeProfitFactor * 投入资金` 时，立即平掉整组仓位。
- 当浮动亏损达到 `10 * TakeProfitFactor * 投入资金` 时触发紧急止损，与 MT4 版本的风险容忍度保持一致。
- 平仓通过反向市价单完成；清仓后重置网格状态，并等待新的轨道触碰信号。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TakeProfitFactor` | `decimal` | `0.05` | 盈利目标系数，乘以投入资金得到平仓阈值。 |
| `Multiplier` | `decimal` | `1.5` | 每次加仓的体积乘数。 |
| `BollingerPeriod` | `int` | `20` | 布林带的计算周期。 |
| `InitialVolume` | `decimal` | `1` | 新网格首单的基础手数，会根据交易所限制自动四舍五入。 |
| `CandleType` | `DataType` | 15 分钟 | 生成信号所使用的 K 线类型。 |

## 与原版 EA 的差异
- StockSharp 采用净头寸模型，本策略通过保存网格成交列表来模拟 MT4 中基于订单票据的持仓管理。
- 体积限制（`Security.MinVolume`、`Security.VolumeStep`、`Security.MaxVolume`）由框架自动应用，替代原代码中的
  `CheckVolumeValue` 函数。
- 信号在收盘后计算，无法逐 Tick 检测，因此通过当前柱的最高价/最低价来近似 MT4 的盘中触发条件。
- 下单始终使用市价单，而 MT4 使用 `OrderSend` 并显式指定 Bid/Ask 价格。

## 使用建议
- 在连接器中提供完整的合约元数据（`PriceStep`、`StepPrice`、`MinVolume`、`VolumeStep`、`MaxVolume`），以便收益、
  风险和体积计算与 MT4 结果保持一致。
- 几何级数加仓具有较高风险，建议在真实部署前使用历史数据充分回测并评估保证金要求。
- 网格宽度直接等于当前布林带宽度，调整 `BollingerPeriod` 会同时改变信号频率和网格间距，优化时需关注敏感度。
