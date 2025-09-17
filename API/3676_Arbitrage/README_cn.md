# 套利策略

## 概述
**Arbitrage Strategy** 复刻自 MetaTrader 专家顾问 `Arbitrage.mq5`。策略同时监听 EURUSD、GBPUSD 与 EURGBP 三个货币对，比较 EURUSD 与 GBPUSD 合成的 EURGBP 合成报价和真实 EURGBP 报价。一旦价差足够覆盖三条腿的手续费与当前点差，就会发送一组三腿对冲市价单以捕捉失衡。

## 交易逻辑
1. 订阅三个货币对的 Level 1 行情，并缓存其买卖价。
2. 在每次行情更新时计算两个合成价格：
   - `syntheticSell = EURUSD_ask / GBPUSD_bid`
   - `syntheticBuy = EURUSD_bid / GBPUSD_ask`
3. 估算交易成本：
   - 三个货币对买卖差的总和。
   - 将手续费（以手数计）通过交叉盘的最小价格变动转换为价格单位。
4. 按交叉盘支持的小数位数对成本取整，并额外加上一点（`PriceStep`）。
5. 当优势超过阈值时开仓：
   - **卖出合成 / 买入真实交叉盘**：卖出 EURUSD、买入 GBPUSD、买入 EURGBP。
   - **买入合成 / 卖出真实交叉盘**：买入 EURUSD、卖出 GBPUSD、卖出 EURGBP。
6. 策略同一时间只允许一个篮子，在开立反向篮子前会平掉相关持仓，保持净头寸为零。

## 参数
| 名称 | 描述 | 默认值 |
| --- | --- | --- |
| `FirstLeg` | 构建合成报价的第一条腿（EURUSD），必填。 | — |
| `SecondLeg` | 构建合成报价的第二条腿（GBPUSD），必填。 | — |
| `CrossPair` | 与合成报价比较的交叉盘（EURGBP），必填。 | — |
| `LotSizePerThousand` | 每 1000 单位账户权益对应的交易手数，控制篮子规模。 | `0.01` |
| `CommissionPerLot` | 三条腿合计的手续费（以手数计）。 | `7` |
| `LogMaxDifference` | 是否记录观测到的最大合成价差。 | `false` |

## 仓位大小
交易手数根据当前账户权益计算：
```
rawVolume = (portfolioValue / 1000) * LotSizePerThousand
volume = round_to_volume_step(rawVolume, CrossPair.VolumeStep)
volume = min(volume, CrossPair.MaxVolume)
```
辅助函数使用交叉盘的成交量步长对结果取整，确保符合经纪商的手数限制。

## 风险提示
- 请确保三只标的使用同一投资组合或保证金账户，否则篮子可能无法全部成交。
- 策略默认市价单即时成交，滑点会直接侵蚀套利优势。
- 必须正确配置品种映射并维持连续行情，过期报价将阻止篮子生成。
