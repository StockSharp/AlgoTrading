# FatPanel 可视化构建器策略

**FatPanel 可视化构建器策略** 是对 MetaTrader FAT Panel 专家顾问的 StockSharp 版本。原始 MQL 程序通过拖拽方式把“信号、逻辑、状态、下单”模块连接在一起，而本实现保留模块化思路，将所有连接关系写在一个 JSON 字符串里，策略启动时读取并构建所需的组件。

## 转换思路

* MQL 版本中的按钮、标签页和计时器调度器全部移除，改为解析 `Configuration` 参数中的 JSON 并生成相应的模块。
* 所有计算都在所选 `CandleType` 的收盘蜡烛上进行，移动平均等指标直接调用 StockSharp 内置实现，不再手动维护缓存。
* 订单模块原来支持选择品种并设置以“点”为单位的止损/止盈。在 StockSharp 中使用策略的主交易品种，通过 `StopLossPoints` 和 `TakeProfitPoints` 两个参数重新提供风险控制，它们会乘以 `Security.PriceStep` 转换成绝对价格距离。
* 时间与星期过滤器沿用面板逻辑；只有在 JSON 中出现 `Bid` 信号时才订阅 Level1 数据，等价于原面板的按需更新模式。

## 参数说明

| 参数 | 作用 |
| --- | --- |
| `CandleType` | 决定策略计算所使用的蜡烛类型/周期。 |
| `Configuration` | 描述规则、条件和动作的 JSON 文本。默认值还原了面板自带的 EMA/SMA 交叉示例。 |
| `Volume` | 如果规则未单独指定成交量，则采用该默认下单量。 |
| `StopLossPoints` | 止损距离（单位为价格步长的倍数），0 表示禁用。 |
| `TakeProfitPoints` | 止盈距离（单位为价格步长的倍数），0 表示禁用。 |

当两个点数参数均大于 0 且标的提供有效 `PriceStep` 时，策略会在 `OnStarted` 中一次性调用 `StartProtection` 激活保护。

## JSON 结构

```json
{
  "rules": [
    {
      "name": "可选规则名称",
      "all": [ /* 必须全部满足的条件 */ ],
      "any": [ /* 至少满足其中一个的条件 */ ],
      "none": [ /* 必须全部不满足的条件 */ ],
      "action": { "type": "Buy" | "SellShort" | "Close", "volume": 1.0 }
    }
  ]
}
```

条件的 `type` 字段支持：

| 类型 | 字段 | 说明 |
| --- | --- | --- |
| `comparison` | `operator`, `left`, `right`, `threshold` | 连接两个信号模块，运算符包含 `Greater`、`Less`、`Equal`、`CrossAbove`、`CrossBelow`。阈值解释为绝对价格差，交叉运算要求上一根蜡烛在另一侧、当前差值超过阈值。 |
| `position` | `required` | 对应面板里的仓位状态模块：`Any`、`FlatOnly`、`FlatOrShort`、`FlatOrLong`、`LongOnly`、`ShortOnly`。 |
| `time` | `start`, `end` | 日内交易时间窗口，格式为 `HH:mm`。若开始时间大于结束时间，则表示跨夜（与 MQL 一致）。 |
| `dayOfWeek` | `days` | 允许交易的星期列表，缺省值为周一至周五。 |

信号模块定义示例：

```json
{ "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" }
{ "type": "Bid" }
{ "type": "Constant", "level": 1.2345 }
```

* `MovingAverage` 支持 `Simple`、`Exponential`、`Smoothed`、`LinearWeighted` 四种算法，并可选择 `Open/High/Low/Close` 作为输入。
* `Bid` 读取最新买价；在收到 Level1 之前临时使用蜡烛收盘价。
* `Constant` 与面板的 HLINE 块等价，提供固定参考值。

动作模块：

* `Buy`：当仓位为空或为空头时买入，必要时自动反手。
* `SellShort`：当仓位为空或为多头时卖出开空，同样支持反手。
* `Close`：调用 `ClosePosition()` 平掉当前仓位。

单个动作可以通过 `volume` 字段覆盖默认下单量。

## 执行流程

1. 策略启动时解析 JSON。若格式错误，会记录错误日志并抛出异常。
2. 每个信号只创建一次指标实例，多个规则引用同一信号时不会重复计算。
3. 每根收盘蜡烛先刷新全部信号，再按顺序评估各规则。`all` 必须全部通过，`any` 至少一个通过，`none` 必须全部不通过。
4. 条件满足后执行对应的市场单，并在日志中记录触发的规则名称。
5. 若设置了止损或止盈，保护逻辑会在 `OnStarted` 中被激活。

## 限制

* 仅支持 `Strategy.Security` 对应的单一标的；原版的跨品种布线需通过多实例实现。
* JSON 已提供 `all`/`any`/`none`，足以表达 AND/OR/NOT，但极端复杂的图形仍需要手动拆分。
* `Cross` 运算只参考上一根蜡烛，与 MQL 中可设“时间窗口与点数”不同；可通过调整 `threshold` 模拟灵敏度。
* 面板中的 UI 交互（拖拽、对话框、工具栏等）在 StockSharp 中没有对应物，被全部省略。

## 默认示例

策略内置的示例与原面板一致：

```json
{
  "rules": [
    {
      "name": "EMA crosses above SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossAbove",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
        { "type": "time", "start": "09:00", "end": "17:00" },
        { "type": "position", "required": "FlatOrShort" }
      ],
      "action": { "type": "Buy" }
    },
    {
      "name": "EMA crosses below SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossBelow",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "position", "required": "LongOnly" }
      ],
      "action": { "type": "Close" }
    }
  ]
}
```

当 20 EMA 上穿 50 SMA 且满足时段/星期/仓位条件时做多；反向交叉且仍持有多单时平仓。
