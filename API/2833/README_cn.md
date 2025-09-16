# EA Moving Average 策略

## 概览
- 由 MetaTrader 专家顾问 **“EA Moving Average”**（barabashkakvn 版）移植而来。
- 使用四条可配置的移动平均线分别负责多空进场与离场判断。
- 适用于单品种、净额结算账户。默认使用 15 分钟K线，可根据需要选择其他标准K线类型。
- 策略始终只保留一张持仓；持仓期间仅检查离场条件，不会反向加仓。

## 交易逻辑
### 做多进场
1. 当前K线开盘价在 *Buy Open* 均线之下、收盘价突破到均线上方（单根K线完成金叉）。
2. `UseBuy` 为真。
3. 若启用 `ConsiderPriceLastOut`，当前价格必须小于或等于上一次平仓价格，避免在更高价位追多。
4. 条件满足时按风险模型计算的仓位执行市价买单。

### 做多离场
1. 仅在存在多头仓位时生效。
2. K线开在 *Buy Close* 均线上方，收盘跌回其下方（死叉信号）。
3. 触发后立即以市价全量平仓。

### 做空进场
1. 当前K线开盘在 *Sell Open* 均线上方，收盘跌破均线。
2. `UseSell` 为真。
3. 若启用 `ConsiderPriceLastOut`，当前价格必须大于或等于上一次平仓价格，避免在更低价位追空。
4. 条件满足时按风险模型执行市价卖单。

### 做空离场
1. 仅在存在空头仓位时生效。
2. K线开盘位于 *Sell Close* 均线下方、收盘站回其上方。
3. 触发后市价全量回补。

## 风险与仓位管理
- `MaximumRisk` 表示每笔交易可承受的账户资金比例。策略将该比例乘以投资组合市值并除以当前价格，得到基础下单数量。
- `DecreaseFactor` 模拟原版EA的“亏损递减”逻辑：连续两笔及以上亏损后，根据亏损次数与 `DecreaseFactor` 的比值按比例减小下单数量。
- 计算出的数量会根据品种的最小成交量步长取整，且不低于一个步长；若风险计算失败，则回退到策略的 `Volume` 属性（默认 1 手）。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `MaximumRisk` | `0.02` | 单笔交易风险占账户净值的比例。 |
| `DecreaseFactor` | `3` | 连续亏损后减少仓位的系数，`0` 表示不启用。 |
| `BuyOpenPeriod` | `30` | 多头进场均线的周期。 |
| `BuyOpenShift` | `3` | 多头进场均线向前平移的K线数量。 |
| `BuyOpenMethod` | `Exponential` | 多头进场均线的类型（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 |
| `BuyOpenPrice` | `Close` | 多头进场均线使用的价格。 |
| `BuyClosePeriod` | `14` | 多头离场均线周期。 |
| `BuyCloseShift` | `3` | 多头离场均线的平移值。 |
| `BuyCloseMethod` | `Exponential` | 多头离场均线类型。 |
| `BuyClosePrice` | `Close` | 多头离场均线使用的价格。 |
| `SellOpenPeriod` | `30` | 空头进场均线周期。 |
| `SellOpenShift` | `0` | 空头进场均线平移。 |
| `SellOpenMethod` | `Exponential` | 空头进场均线类型。 |
| `SellOpenPrice` | `Close` | 空头进场均线使用的价格。 |
| `SellClosePeriod` | `20` | 空头离场均线周期。 |
| `SellCloseShift` | `2` | 空头离场均线平移。 |
| `SellCloseMethod` | `Exponential` | 空头离场均线类型。 |
| `SellClosePrice` | `Close` | 空头离场均线使用的价格。 |
| `UseBuy` | `true` | 是否允许做多。 |
| `UseSell` | `true` | 是否允许做空。 |
| `ConsiderPriceLastOut` | `true` | 新开仓前是否需要相对上次平仓获得更优价格。 |
| `CandleType` | 15 分钟K线 | 参与计算的K线类型。 |

## 额外说明
- 最新一次平仓价格与连续亏损计数来自真实成交回报，复现了原 EA 的行为。
- StockSharp 在K线收盘时触发逻辑，因此进场价格过滤使用的是收盘价，对应原代码中基于实时买卖价的判断。
- 策略假设账户为净额模式，不支持同时持有多空仓位。
- 建议先在历史数据或模拟环境中验证参数，再投入实盘。 
