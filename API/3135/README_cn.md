# Advanced EA Panel Strategy

该策略将 MQL5 的 **Advanced EA Panel** 手动交易面板移植到 StockSharp 平台。原始面板提供多周期指标、枢轴点、风险评估以及一键交易。现在所有计算都由 C# 策略自动完成，并通过日志与参数呈现，方便接入自动化流程或自建界面。

## 主要特性

- 同时订阅 9 个周期（M1 … MN1），跟踪 EMA(3/6/9)、SMA(50/200)、CCI(14)、RSI(21) 的方向性投票。
- 根据 `PivotFormula` 选择 Classic、Woodie 或 Camarilla 枢轴点公式，并在指定周期上重新计算。
- 通过 ATR 监控波动率，数值变化时在日志中输出最新读数。
- 维护风险面板：保存进场价、止损、止盈，并计算风险、收益距离以及 R/R 比例。
- 多周期投票超过 `DirectionalThreshold` 时可自动下单。若持有反向仓位，先调用 `ClosePosition()` 平仓，再发送新的市价单。
- 借助 `StartProtection` 创建随策略一起恢复的止损/止盈保护，忠实再现原面板的防护逻辑。

## 交易流程

1. 每个周期的订阅通过 `Bind` 获取指标值。当收盘价高于所有均线、CCI>+100、RSI>60 时计入一个看涨票数；若条件反向则记为看跌票，其他情况不计分。
2. 将所有已就绪周期的票数求和。结果 ≥ `DirectionalThreshold` 时生成买入信号，≤ -`DirectionalThreshold` 时生成卖出信号。
3. `AutoTradingEnabled = true` 时：
   - 若当前持有反向仓位，先调用 `ClosePosition()` 退出。
   - 之后按 `Volume`（四舍五入到 `VolumeStep`）发送市价单，并通过 `StartProtection` 挂出基于点差的止损/止盈。
4. 主周期的 ATR 每次发生可识别的变化都会写入日志，便于监测波动率环境。
5. 枢轴点在所选周期的 K 线收盘后重新计算，日志中给出 PP、R1–R4、S1–S4，可用于自定义面板或预警系统。

## 参数说明

| 名称 | 说明 | 分组 | 默认值 |
| --- | --- | --- | --- |
| `Volume` | 交易手数，发送订单前会按 `VolumeStep` 调整。 | Trading | 1.0 |
| `StopLossPips` | 止损距离（价格步长数），设为 0 则不挂止损。 | Risk | 50 |
| `TakeProfitPips` | 止盈距离（价格步长数），设为 0 则不挂止盈。 | Risk | 100 |
| `VolatilityPeriod` | ATR 计算周期。 | Volatility | 14 |
| `PrimaryCandleType` | 用于 ATR 和图表绘制的 K 线类型。 | General | 15 分钟 |
| `PivotCandleType` | 计算枢轴点时使用的 K 线类型。 | General | 1 小时 |
| `DirectionalThreshold` | 触发交易所需的绝对票数。 | Signals | 3 |
| `AutoTradingEnabled` | 是否自动执行信号。 | Signals | true |
| `PivotFormula` | 枢轴点公式（Classic、Woodie、Camarilla）。 | General | Classic |

## 风险控制

- `StartProtection` 把点差参数换算成绝对价格，并创建随策略维护的保护单。
- `_entryPrice`、`_stopPrice`、`_takePrice` 在成交时更新，随后计算风险、收益及 R/R 比例并写入日志。
- 即便关闭自动交易，风险面板仍会跟踪外部或手动执行的仓位。

## 与 MQL5 版本的差异

- 不再绘制 UI 控件，所有结果通过日志与参数暴露；需要图形界面时可在 StockSharp 中自行订阅这些数据。
- 面板按钮 (`Buy`, `Sell`, `Reverse`, `Close`) 改写为 `RequestExecution`、`SendOrder`、`ClosePosition()` 等方法，保持同样的执行顺序。
- 原有的 Points of Interest、文本编辑框、拖拽线条未迁移；改为在代码里重新计算枢轴点，必要时可以扩展成绘图逻辑。
- 波动率与风险指标不依赖图表对象，而是按需重新计算，因此重启后不会丢失。

## 使用建议

1. 连接策略后确认数据源能提供 `PanelTimeFrames` 中列出的全部周期，否则信号生成会延迟。
2. 调整 `DirectionalThreshold` 以匹配偏好的信号敏感度；阈值越大，越需要多周期一致才会触发。
3. 将 `AutoTradingEnabled` 设为 `false` 时，可把策略当作信息面板使用，交易动作由其他系统完成。
4. 代码默认绘制主周期的蜡烛、ATR 以及成交点，可按需要移除或扩展，以适应自定义可视化。

## 移植要点

- **操作映射**：`EAPanelClickHandler` 等回调映射为策略内部的下单与风控函数，复现买入、卖出、反手与平仓流程。
- **枢轴点公式**：保留面板的预设组合（Classic/Woodie/Camarilla），便于与原策略对照。
- **指标替换**：使用 StockSharp 的 `ExponentialMovingAverage`、`SimpleMovingAverage`、`CommodityChannelIndex`、`RelativeStrengthIndex` 处理多周期数据。
- **风险日志**：原面板输入框显示的信息，现在全部写入日志，方便外部系统订阅。

借助这些改动，Advanced EA Panel 的市场洞察和操作流程被完整保留，同时具备 StockSharp 策略的自动化与可优化特性。
