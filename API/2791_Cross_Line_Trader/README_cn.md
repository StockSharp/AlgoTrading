# Cross Line Trader 策略

## 概述
该策略复刻了 MetaTrader 中的 “Cross Line Trader” 专家顾问，通过监控用户预先定义的合成线与价格的交互来下单。StockSharp 版本不再监听图表对象，而是从一个参数中读取所有线条描述，在启动时解析，并在每根收盘 K 线到来时持续监测。当新的 K 线开盘价穿越某条仍然有效的线时，策略会按照对应方向发送市价单，并将该线标记为不可再触发。

## 交易逻辑
1. 通过 **Candle Type** 参数订阅指定的 K 线类型，只处理 `Finished` 状态的 K 线以避免盘中噪声。
2. 启动后根据 **Line Definitions** 参数创建合成线，每条线都维护自身的活动状态、已处理的条数以及几何信息。
3. 对于 **Trend** 或 **Horizontal** 类型的线，算法比较上一根 K 线开盘价与当前开盘价相对于线条轨迹的位置：
   - 当上一根开盘价位于线下方，而当前开盘价突破至线上方时触发做多。
   - 当上一根开盘价位于线上方，而当前开盘价跌破至线下方时触发做空。
4. **Vertical** 线等同于定时触发器。在达到预设的条数后，策略会在当根 K 线的开盘价立刻开仓。
5. 交易方向由 **Direction Mode** 决定：
   - `FromLabel` 会把线条标签与 **Buy Label** / **Sell Label** 比较。
   - `ForceBuy` 与 `ForceSell` 忽略标签，将所有线统一视为一个方向。
6. 每次成功触发都会按照 **Trade Volume** 设定的数量下市价单，写入日志，并将该线设为非活动状态。
7. 若设置了止损或止盈距离，策略会在每根新 K 线上根据最新持仓价格以及当根的最高 / 最低价来判断是否需要平仓。

## 线条定义格式
**Line Definitions** 字符串使用分号分隔多条线，每条线需遵循以下格式：

```
Name|Type|Label|BasePrice|SlopePerBar|Length|Ray
```

- **Name**：日志中显示的名称，可任意填写但不能包含分号。
- **Type**：`Horizontal`、`Trend` 或 `Vertical`（大小写不敏感）。
- **Label**：自由文本，在 `FromLabel` 模式下与 **Buy Label**/**Sell Label** 匹配。
- **BasePrice**：第一根处理的 K 线对应的初始价格，所有非垂直线都必须提供（使用十进制，采用不变文化写法）。
- **SlopePerBar**：趋势线的每根 K 线价格增量；水平线填 `0`。
- **Length**：含义取决于线条类型：
  - 对于没有 Ray 的趋势线或水平线，表示右端点距离起点的 K 线数量，超过后该线自动失效。
  - 若 Ray 为 `true`，该值被忽略，线将无限延伸。
  - 对于垂直线，表示等待多少根 K 线后触发，最小值为 `1`。
- **Ray**：`true` 表示向右无限延伸，`false` 表示仅在 `Length` 范围内有效。

示例：

```
TrendLine|Trend|Buy|1.1000|0.0005|8|false;HorizontalSell|Horizontal|Sell|1.1050|0|0|true;VerticalImpulse|Vertical|Buy|0|0|1|false
```

该示例包含一条上升趋势买线、一条永久有效的水平卖线以及一条下一根 K 线立即触发的垂直线。

## 参数说明
- **Candle Type**：用于计算的行情数据类型，默认 1 分钟 K 线。
- **Trade Volume**：开仓市价单的数量，必须为正值。
- **Direction Mode**：决定如何确定进场方向，可选 `FromLabel`、`ForceBuy`、`ForceSell`。
- **Buy Label / Sell Label**：在 `FromLabel` 模式下用来识别买卖线条的标签文本。
- **Line Definitions**：描述所有合成线的原始字符串（格式如上）。
- **Stop Loss Offset**：以价格单位表示的止损距离，设置为 0 表示禁用。
- **Take Profit Offset**：以价格单位表示的止盈距离，设置为 0 表示禁用。

## 风险控制
策略不会单独挂出止损或止盈委托，而是在每根收盘 K 线上检查：
- 多头持仓：若最低价低于 `EntryPrice - StopLossOffset`，或最高价超过 `EntryPrice + TakeProfitOffset`，则市价平仓。
- 空头持仓：若最高价高于 `EntryPrice + StopLossOffset`，或最低价低于 `EntryPrice - TakeProfitOffset`，则市价平仓。

当两项距离均为 0 时，仓位仅会由反向信号或人工操作关闭。

## 实现细节
- 代码中的注释均采用英文，符合项目规范。
- 格式错误的线条定义会被忽略，不会报错，请确保文本填写正确。
- 重启策略会重置内部状态，线条计数与触发计时从第一根新 K 线重新开始。
- 与原始 EA 一样只关注开盘价，盘中触碰不会触发交易。

## 使用步骤
1. 配置交易标的及所需的 K 线类型。
2. 根据需求填写 **Line Definitions**，定义所有希望交易的线条。
3. 选择适当的 **Direction Mode**，决定是否使用标签区分方向。
4. 可选地设置止损和止盈距离，实现自动退出。
5. 启动策略并查看日志，每次触发都会记录线条名称、方向及触发价格。
