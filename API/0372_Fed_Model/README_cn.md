# 美联储模型策略
[English](README.md) | [Русский](README_ru.md)

该宏观时机策略比较股票市场盈利收益率与10年期美国国债收益率。当股票收益率更高时持有股票ETF；当国债收益率更高时转入现金。每月对收益率差进行回归并预测下个月的值，从而减少噪声切换。

每个月末算法利用最近`RegressionMonths`个月的数据预测下一月的收益差。如果预测值为正则买入股票ETF，否则持有现金代理。只有当预测穿越零点时才调整仓位，降低换手率。

## 细节

- **入场条件**：
  - 月末对 `(EarningsYield - BondYield)` 的 `RegressionMonths` 个观察值做回归并预测下一期。
  - 如果预测为正且订单金额 ≥ `MinTradeUsd`，买入股票ETF。
- **多空方向**：只做多股票或现金。
- **出场条件**：当预测的收益差为负时卖出股票ETF。
- **止损**：无。
- **默认参数**：
  - `Universe` – [股票ETF，可选现金ETF]。
  - `BondYieldSym` – 10年期国债收益率序列。
  - `EarningsYieldSym` – 股票市场盈利收益率。
  - `RegressionMonths` = 12。
  - `CandleType` = 1天。
  - `MinTradeUsd` – 最小交易金额。
- **筛选**：
  - 类型：宏观。
  - 方向：仅多头。
  - 周期：每月。
  - 再平衡：每月。

