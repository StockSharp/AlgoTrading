# Rsi Test 策略

## 概览
`RsiTestStrategy` 将 MetaTrader 4 专家顾问 **RSI_Test** 迁移到 StockSharp 的高级 API。策略结合 RSI 动能判定、K 线开盘价确认以及基于风险的仓位控制，只在 K 线完成后运行，与原始 EA 的收盘判定逻辑完全一致。

## 交易规则
1. 使用参数 `RsiPeriod` 计算 RSI。
2. 当 RSI 从超卖区 (`BuyLevel`) 向上反弹，且当前 K 线开盘价高于上一根 K 线时开多。
3. 当 RSI 从超买区 (`SellLevel`) 向下回落，且当前 K 线开盘价低于上一根 K 线时开空。
4. 遵守 `MaxOpenPositions` 限制。数值为 `0` 表示无限制，否则净头寸不得超过 `MaxOpenPositions * Volume`。
5. 通过阶梯式拖尾止损离场：价格自均价移动 `TrailingDistanceSteps` 个最小跳动后，止损移动到相同距离，并保持不变。
6. 不设置固定止盈；仓位仅在拖尾止损被触发或策略停止时退出。

## 仓位与风险控制
* 策略按照 `RiskPercentage` 的账户权益估算下单量。若证券提供 `Security.MarginBuy`/`Security.MarginSell`，则基于单手保证金计算；否则退化为用最新收盘价估算所需资金。
* 下单量向 `Security.VolumeStep` 对齐（若未知则保留两位小数），同时限制在 `Security.MinVolume` 与 `Security.MaxVolume` 范围内。
* 将 `RiskPercentage` 设为 `0` 可以关闭动态仓位管理，此时始终使用参数 `Volume`。

## 拖尾止损逻辑
* `TrailingDistanceSteps` 以价格最小跳动 (`Security.PriceStep`) 表示；若缺少该信息，则视为绝对价格偏移。
* 价格突破触发阈值（多头为 `均价 + 距离`，空头为 `均价 - 距离`）后，止损立即移动到同等距离处，仅执行一次，与原 EA 的“一级阶梯”逻辑一致。

## 参数列表
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `RsiPeriod` | RSI 周期。 | `14` |
| `BuyLevel` | 触发多头的超卖阈值。 | `12` |
| `SellLevel` | 触发空头的超买阈值。 | `88` |
| `RiskPercentage` | 按账户权益计算下单量的百分比，`0` 表示禁用。 | `10` |
| `TrailingDistanceSteps` | 激活拖尾止损所需的价格跳动数。 | `50` |
| `MaxOpenPositions` | 最大同时持仓数，`0` 为不限。 | `1` |
| `CandleType` | 计算使用的主时间框架。 | `15` 分钟 |
| `Volume` | 当风险参数不可用时的备用手数。 | `1` |

## 使用建议
1. 建议选择包含正确 `PriceStep`、`VolumeStep` 和保证金信息的品种，以获得与 MT4 接近的结果。
2. 策略仅处理已完成的 K 线 (`CandleStates.Finished`)，测试与实盘应使用相同的时间框架。
3. 在 `OnStarted` 中调用了 `StartProtection()`，可利用 StockSharp 自带的保护机制处理异常仓位。
4. 原 EA 中通过全局变量触发的自动优化已移除，所有参数需在 StockSharp 中手动配置。
5. 若组合能实时更新 `Portfolio.CurrentValue`，动态仓位计算才能生效；否则系统将回退到固定 `Volume`。
