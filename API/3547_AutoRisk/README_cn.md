# AutoRisk 策略

## 概述
AutoRisk 策略复刻了原始 MetaTrader 智能交易系统的仓位管理逻辑。它订阅日线级别的K线，计算 14 周期的平均真实波幅（ATR），并依据账户权益或余额得出推荐下单手数。策略本身不会主动发单，而是通过 `RecommendedVolume` 属性保存最近一次计算结果，并在日志中输出提示。

## 核心逻辑
1. 订阅设定的蜡烛类型（默认日线），并将数据传入 `AverageTrueRange` 指标。
2. 在蜡烛收盘后，利用 `PriceStep` 将 ATR 转换为价格步数，再通过 `StepPrice` 换算成货币金额。
3. 将风险百分比 (`RiskFactor`) 作用于所选账户指标 (`CalculationMode`)，并除以基于 ATR 的分母，获得原始手数。
4. 按照交易所规则 (`VolumeStep`, `MinVolume`, `MaxVolume`) 调整手数，`RoundUp` 用于决定是四舍五入到最近步长还是向下取整。
5. 保存并记录最终手数，方便外部组件获取该数值并执行下单。

## 参数说明
- **RiskFactor**：作用于 ATR 分母的风险百分比（默认 `2`）。
- **CalculationMode**：选择用于计算的账户指标：`Equity`（当前权益）或 `Balance`（初始余额，默认）。
- **RoundUp**：启用时按照最近的手数步长四舍五入，否则向下截断（默认 `true`）。
- **CandleType**：用于 ATR 计算的行情类型（默认日线 `TimeFrameCandleMessage`）。

## 数据要求
- 绑定包含 `PriceStep`、`StepPrice`、`VolumeStep`、`MinVolume`、`MaxVolume` 的标的，以便遵守交易规则。
- 连接会更新 `Portfolio.CurrentValue`（权益）与 `Portfolio.BeginValue`（余额）的投资组合适配器。
- 提供日线数据流，或按需调整 `CandleType` 至其他周期。

## 使用步骤
1. 启动前为策略指定证券与投资组合。
2. 启动策略后即会订阅 ATR 所需的蜡烛序列，无需手动注册指标。
3. 通过读取 `RecommendedVolume` 或查看信息日志，获取最新推荐手数。
4. 在外部下单逻辑中引用该手数执行交易。
