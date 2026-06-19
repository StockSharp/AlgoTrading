# Swaper 策略 (API 3751)

## 概述

**Swaper Strategy** 通过 StockSharp 高级 API 复刻 MetaTrader 顾问 "Swaper 1.1"。原始策略依靠在多头与空头头寸之间来回
调仓来获取掉期收益。本移植版本保留了这种资金流逻辑：重建虚拟账户余额、计算标的的公允价值，并把当前头寸调整到
该目标附近。

## 核心逻辑

1. **重建虚拟资金。** 组合 `money` 变量 = 初始资金 (`BaseUnits * BeginPrice`) + 已实现盈亏 + 当前头寸的浮动盈亏
   （乘以 `ContractMultiplier`）。
2. **公允价值的分母。** MQL 中的 `com` 会随持仓变化而增减。移植版使用 `BaseUnits + ContractMultiplier * Position`
   来保持相同的效果。
3. **目标数量计算。** 取最近两根蜡烛的最高价（加上市场价差）和最低价，复现原策略的保护逻辑，并使用
   `Experts / (Experts + 1)` 控制调整力度。
4. **调整头寸。** 根据 `dt` 的结果：
   - 若目标增量小于 0.1 手，则直接平仓；
   - 当 `dt < 0` 时增加空头或减少多头；
   - 当 `dt >= 0` 时增加多头或减少空头。
5. **保证金控制。** `GetTradableVolume` 使用 `MarginPerLot` 与组合当前价值近似 `AccountFreeMargin()`。若保证金不足，
   数量会被向下取整到 0.1 手。

整个流程在每根完结的蜡烛上执行，替代原脚本的逐笔 `start()` 循环，同时保持策略含义。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Experts` | `1` | 控制向公允价值靠拢的权重。 |
| `BeginPrice` | `1.8014` | 重建虚拟余额时使用的起始价格。 |
| `MagicNumber` | `777` | 保留的 MetaTrader 标识，可用于下单备注。 |
| `BaseUnits` | `1000` | 公允价值分母中的基础资金单位。 |
| `ContractMultiplier` | `10` | 将价格差转换为账户货币的乘数。 |
| `MarginPerLot` | `1000` | 持有 1 手所需的近似资金，用于限制下单量。 |
| `FallbackSpreadSteps` | `1` | 当无 Level 1 报价时使用的价差步数。 |
| `CandleType` | `1 小时` | 执行再平衡所用的主时间框架。 |

## 运行流程

1. 订阅配置好的蜡烛序列和 Level 1 数据，以便获得价差。
2. 如果缺少报价，则使用 `FallbackSpreadSteps * PriceStep` 估算价差。
3. 在每根完结蜡烛上重新计算虚拟资金和分母 `com`。
4. 先按照最高价路径计算 `dt`，若 `dt < 0`，切换到最低价路径以复制原策略的防护逻辑。
5. 调用 `AdjustShort` 或 `AdjustLong` 调整仓位；若目标小于 0.1 手，直接平仓以模拟 MetaTrader 的 `closeby` 行为。
6. 在 `OnOwnTradeReceived` 中累积已实现盈亏，确保下一次循环使用最新余额。

## 与 MQL4 版本的差异

- 将逐笔 `start()` 循环替换为蜡烛事件，避免忙等待同时保持策略思想。
- 订单历史与持仓扫描通过策略自身的成交流实现，替代 `OrdersHistoryTotal()` 和 `OrdersTotal()`。
- 保证金检查使用 `Portfolio.CurrentValue` 与可配置的 `MarginPerLot`，因为 StockSharp 中没有经纪商特定的
  `MarketInfo` 接口。
- `OrderCloseBy` 被净头寸平仓模拟，这符合大多数 StockSharp 连接器的净额模式。

## 使用建议

- 根据交易所/经纪商合约调整 `MarginPerLot`，防止申请超出保证金的数量。
- 选择与原策略接近的时间框架，以保持蜡烛高低点的一致性。
- 确保蜡烛与 Level 1 订阅同时启用，使价差估算更准确，行为更加贴近原始脚本。
