# 挂单稳定策略

## 概述
**挂单稳定策略** 是对 MetaTrader 专家顾问 `hjueiisyx8lp2o379e_www_forex-instruments_info.mq4` 的移植。原策略会在现价上下放置一对止损挂单，并等待价格突破。一旦触发开仓，系统会监控最近几根蜡烛的实体大小，用来判断行情是否进入横盘，并在动能减弱或利润达到预设目标时退出仓位。

该 C# 版本使用 StockSharp 的高级 API，只在蜡烛收盘后做出决策，从而保证回测与实盘表现的一致性。

## 交易规则
1. 当没有持仓或挂单时，策略会在收盘价上方放置 **buy stop**，下方放置 **sell stop**，间距为 `OrderDistancePoints` 个 MetaTrader 点。
2. 任一止损挂单被触发时：
   - 以 `OrderVolume` 手的固定仓位开仓；
   - 另一侧的止损挂单保留，用于捕捉反向突破。
3. 持仓期间持续检查最近两根已完成蜡烛的实体：
   - 如果最新蜡烛实体小于 `StabilizationPoints`，且浮动利润大于 `ProfitThreshold`，则平仓并取消对侧挂单；
   - 如果连续两根蜡烛都小于 `StabilizationPoints`，无论盈亏都立即平仓；
   - 当利润达到 `AbsoluteFixation` 时立刻平仓。
4. 若 `ExpirationMinutes` 大于零，则挂单在到期后取消并重新创建。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `OrderVolume` | 每次交易的手数。 | `0.1` |
| `OrderDistancePoints` | 与当前价格的距离（MetaTrader 点）。 | `20` |
| `ProfitThreshold` | 允许根据“稳定”信号离场所需的最低浮动利润（账户货币）。 | `-2` |
| `AbsoluteFixation` | 达到该利润（账户货币）时无条件平仓。 | `30` |
| `StabilizationPoints` | 判断市场进入盘整的最大蜡烛实体（点）。 | `25` |
| `ExpirationMinutes` | 挂单有效期（分钟），`0` 表示无限期。 | `20` |
| `CandleType` | 用于判断稳定性的蜡烛类型（默认 5 分钟）。 | `TimeFrame(5m)` |

## 转换说明
- 原始程序基于逐笔行情；移植版本只处理已完成蜡烛，使结果更易复现。
- MetaTrader 的“点”映射到 StockSharp 的 `PriceStep`，若无定义则使用 `1`。
- 利润通过 `PriceStep` 与 `StepPrice` 换算为账户货币的估算值。
- 代码中的注释与参数描述全部改写为英文，以符合仓库要求。

## 使用方法
1. 将策略添加到 StockSharp 项目中，并指定交易品种与账户。
2. 根据交易品种调整参数，尤其是蜡烛周期与挂单间距。
3. 启动策略，系统会自动下达成对的止损挂单，并按上述规则管理仓位。

## 可扩展方向
- 根据市场波动性调整蜡烛周期，以在敏感度与噪音过滤之间取得平衡。
- 引入 ATR、布林带等波动性过滤器，避免在极端平静的时段交易。
- 在接近目标利润时加入移动止损或分批减仓机制。
