# Risk Fixed Margin 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 5 脚本 `risk (barabashkakvn's edition).mq5` 的 StockSharp 版本。原脚本不会下单，只计算在固定风险百分比下可以使用多少自由保证金。移植后的策略保持相同目标：订阅 Level 1 报价，估算可用资金，并在日志中输出买入与卖出的建议手数。

## 概览

- **用途：** 监控自由保证金并把它转换成多空方向的建议仓位规模。
- **数据来源：** 仅订阅所选标的的 Level 1（最优买价/卖价）。
- **交易行为：** 策略不会自动发送任何订单，仅提供信息提示。
- **输出内容：** 当数值变化时，在日志中生成包含仓位和账户指标的多行文本。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `Risk %` | 允许用于新仓位的自由保证金百分比。 | `5` |

## 运行流程

1. 启动后订阅 Level 1 数据，以便持续获取最优买价和卖价；不需要额外的 K 线或指标订阅。
2. 每次 Level 1 更新都会刷新缓存的 bid/ask。只要至少一侧价格有效，就会生成一次状态报告。
3. 报告中的账户指标按照以下方式估算：
   - **Equity** 直接使用 `Portfolio.CurrentValue`（当前账户总价值）。
   - **Balance** 通过 `equity - PnL` 计算，从而扣除策略的浮动盈亏，接近 MetaTrader 中的“余额”。
   - **Free margin** 将当前持仓的名义价值（`abs(Position) * mid price`，mid price 为买卖价平均或已知的一侧）从 equity 中扣除。
4. 用自由保证金乘以 `Risk %` 得到风险预算（货币金额）。
5. 对买入和卖出分别计算原始手数 `risk / price`，并向下取整到 `Security.VolumeStep` 的整数倍，确保符合交易所/券商的手数规则。
6. 生成五行日志：第一行提示风险比例，接下来两行给出多头信息，最后两行给出空头信息。只有当结果变化时才会输出新的日志条目。

## 输出示例

```
5% risk of free margin
Check open BUY: 0.42, Balance: 10000.00, Equity: 10050.00, FreeMargin: 9950.00
trade BUY, volume: 0.40
Check open SELL: 0.41, Balance: 10000.00, Equity: 10050.00, FreeMargin: 9950.00
trade SELL, volume: 0.40
```

示例展示了理论最大手数（`Check open ...`）以及按手数步长调整后的实际建议（`trade ...`）。

## 与 MetaTrader 版本的差异

- StockSharp 的投资组合属性与 MetaTrader 账户不同，因此余额与自由保证金基于 `Portfolio.CurrentValue`、策略盈亏和当前持仓名义价值进行估算。
- MetaTrader 中的 `CMoneyFixedMargin` 管理类被显式公式取代，仍旧以自由资金的百分比定义风险，同时方便二次开发。
- 由于缺少 `CTrade.CheckVolume` 的直接替代，手数仅向下对齐到 `VolumeStep` 的倍数。如需考虑最大手数或保证金分层等限制，可在 `CalculateVolumes` 或 `NormalizeVolume` 中扩展逻辑。
- 原脚本调用 `Comment` 更新图表文本，这里改为使用 `LogInfo` 写入策略日志，可在 Designer 或 Runner 的日志窗口查看。

## 使用建议

1. 将策略连接到具有正确价格步长和手数步长设置的投资组合与标的。
2. 根据自身风险偏好调整 `Risk %`。在高杠杆市场中使用较大百分比时，理论仓位可能非常大。
3. 如需更多约束（例如固定最大仓位或额外的保证金检查），可扩展 `CalculateVolumes` 或 `NormalizeVolume`。
4. 本策略只提供信息提示，不会自动下单，可与执行策略结合或手动参考其建议。
