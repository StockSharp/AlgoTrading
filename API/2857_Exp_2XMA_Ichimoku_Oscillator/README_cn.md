# Exp 2XMA Ichimoku 振荡策略

该策略复刻了 MetaTrader 专家顾问 “Exp_2XMA_Ichimoku_Oscillator” 的逻辑，通过两条平滑处理的 Ichimoku 型中枢线来生成进出场信号，并使用 StockSharp 的高级策略 API 来管理仓位。

## 核心思想

1. 在所选周期上计算两条类 Donchian 中枢：
   - **快速中枢** 使用 `UpPeriod1`、`DownPeriod1` 回溯期内的最高价与最低价求平均。
   - **慢速中枢** 使用 `UpPeriod2`、`DownPeriod2` 回溯期重复同样的计算。
2. 通过 `Method1`、`Method2` 指定的移动平均对两条中枢进行平滑，长度分别由 `XLength1`、`XLength2` 控制。可选的平滑类型包括简单、指数、平滑和线性加权移动平均。
3. 震荡值等于两条平滑中枢的差值。其状态根据斜率和符号划分为五种颜色：
   - `PositiveRising` (0)：大于零且继续上升。
   - `PositiveFalling` (1)：大于零但动能减弱。
   - `NegativeRising` (3)：小于零但向零值回升。
   - `NegativeFalling` (4)：小于零且继续下行。
   - `Neutral` (2)：初始化阶段尚未形成明确趋势。
4. 策略仿照 MQL 指标缓冲区的偏移方式，读取 `SignalBar` 指定的历史 K 线颜色以及再往前一根 (`SignalBar + 1`) 的颜色来判断信号。

## 交易规则

- **做多开仓**：当 `EnableBuyOpen` 为真且更早一根柱 (`SignalBar + 1`) 的颜色属于上升状态（0 或 3），而最近的柱 (`SignalBar`) 变为下降状态（1 或 4）时，策略会在 `EnableSellClose` 允许的情况下平掉空头，并按照 `Volume + |Position|` 的数量开或加多单。
- **做空开仓**：当 `EnableSellOpen` 为真且更早一根柱颜色为下降状态（1 或 4），最近一根柱转为上升状态（0 或 3）时，策略在 `EnableBuyClose` 允许时平掉多头，并以 `Volume + |Position|` 的数量开或加空单。
- 所有信号都在触发 K 线收盘时执行，使用市价单，无额外的止损或止盈——离场完全依赖颜色反转。
- 启动时调用 `StartProtection()`，利用框架提供的仓位保护逻辑防止异常持仓。

## 参数说明

| 参数 | 含义 | 默认值 |
| ---- | ---- | ------ |
| `CandleType` | 指标计算所用的时间框架。 | 4 小时 |
| `UpPeriod1`、`DownPeriod1` | 快速中枢的最高/最低回溯长度。 | 6、6 |
| `UpPeriod2`、`DownPeriod2` | 慢速中枢的最高/最低回溯长度。 | 9、9 |
| `XLength1`、`XLength2` | 两条平滑移动平均的长度。 | 25、80 |
| `Method1`、`Method2` | 平滑类型（简单、指数、平滑、线性加权）。 | 简单 |
| `SignalBar` | 读取颜色的历史柱偏移量。 | 1 |
| `EnableBuyOpen`、`EnableSellOpen` | 是否允许做多/做空开仓。 | true |
| `EnableBuyClose`、`EnableSellClose` | 是否允许平多/平空。 | true |
| `Volume` | 基础下单数量，反向时会加上当前仓位的绝对值。 | 1 |

## 使用提示

- 为了兼容 StockSharp，原版自定义 XMA 的相位等高级参数被常见的移动平均类型所替代，但仍能体现策略的趋势/反转特性。
- 策略基于收盘价计算，因此保持了 MQL 版本一根 K 线的确认延迟（`SignalBar = 1`）。需要更多确认时可增大该参数。
- 由于退出完全依赖震荡颜色的变化，实盘运行时建议搭配账户级风控或独立的止损机制。
