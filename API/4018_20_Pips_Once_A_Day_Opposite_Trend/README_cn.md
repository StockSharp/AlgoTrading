# TwentyPipsOnceADayStrategy

该策略基于 MetaTrader 专家顾问 **20pipsOnceADayOppositeLastNHourTrend**，并使用 StockSharp 高阶 API 重新实现。策略每天在指定的小时执行一次交易，通过比较最近一小时与 `N` 小时前的收盘价，逆势开仓。仓位规模采用阶梯式马丁格尔，只在最近的交易出现亏损时才放大手数；同时还增加了交易时段过滤、可选的移动止损以及最大持仓时间限制。

## 交易逻辑

1. 订阅指定周期的蜡烛（默认 1 小时，可通过 `CandleType` 修改）。
2. 当一根蜡烛收盘且下一根蜡烛的小时数等于 `TradingHour` 时执行信号判断：
   - 取最近一根完整蜡烛的收盘价，与 `HoursToCheckTrend` 小时前的收盘价比较。
   - 如果价格下跌，则在新小时开多仓；如果价格上涨，则开空仓。
3. 同一时间仅允许存在一笔仓位（由 `MaxOrders` 控制，默认 1）。
4. 每笔交易都会附带固定止盈、可选止损与移动止损，距离均以点数（pip）定义，并自动转换为品种价格单位。
5. 若持仓时间超过 `OrderMaxAgeSeconds`，或下一小时不在 `TradingDayHours` 指定的交易时段内，则立即平仓。

## 资金管理

- `FixedVolume` 为基准手数。设置为 `0` 时启用风险百分比计算，按照 `(账户价值 * RiskPercent) / 1000` 计算手数，复刻原版 EA 的做法。
- 计算得到的手数会同时受到交易品种的 `VolumeMin/VolumeMax/VolumeStep` 以及参数 `MinVolume` / `MaxVolume` 的限制。
- 马丁格尔阶梯仅在相应历史交易为亏损时生效：
  - 最近一笔亏损时使用 `FirstMultiplier`；
  - 最近一笔盈利但倒数第二笔亏损时使用 `SecondMultiplier`；
  - 依次类推直到 `FifthMultiplier`，与原始 EA 的五级扩仓一致。

## 参数说明

| 参数 | 说明 |
|------|------|
| `FixedVolume` | 固定手数；设为 `0` 启用风险百分比计算。 |
| `MinVolume` / `MaxVolume` | 计算后手数的最小值与最大值限制。 |
| `RiskPercent` | 当 `FixedVolume = 0` 时，根据账户价值换算手数的百分比。 |
| `MaxOrders` | 同时允许的最大持仓数量（默认 1）。 |
| `TradingHour` | 允许开仓的小时（0-23）。 |
| `TradingDayHours` | 允许持仓的小时集合，可写成逗号分隔或区间（例如 `0-7,13-22`）。下一小时不在集合内时强制平仓。 |
| `HoursToCheckTrend` | 反向交易所使用的小时回溯长度。 |
| `OrderMaxAgeSeconds` | 持仓时间上限（秒）。 |
| `FirstMultiplier` … `FifthMultiplier` | 针对最近五笔亏损交易的马丁格尔倍率。 |
| `StopLossPips` | 初始止损距离（pip），设为 `0` 关闭。 |
| `TrailingStopPips` | 移动止损距离（pip），设为 `0` 关闭。 |
| `TakeProfitPips` | 止盈距离（pip）。 |
| `CandleType` | 信号所用蜡烛类型，默认一小时。 |

## 风险控制与离场

- **止盈/止损**：使用 `TakeProfitPips`、`StopLossPips` 配置，自动转换为价格单位。
- **移动止损**：当浮盈超过设定点数时，将止损向有利方向移动。
- **超时平仓**：仓位持有时间超过 `OrderMaxAgeSeconds` 时按当前蜡烛收盘价离场。
- **时段过滤**：下一小时不在 `TradingDayHours` 内时立即平仓。

## 使用建议

- 适用于任意提供小时蜡烛数据且定义了 `PriceStep` 的标的；若标的报价带有 3 或 5 位小数，策略会自动换算点值。
- 若希望贴近原版 EA，请保持 `CandleType` 为 1 小时并将 `TradingDayHours` 设为完整的 `0-23`。策略会在指定小时的开盘价附近成交。
- 马丁格尔阶梯最多参考最近五笔历史结果，重置策略会清空该记录。
- 本项目仅提供 C# 版本，暂未实现 Python 版本。

## 文件结构

- `CS/TwentyPipsOnceADayStrategy.cs`：C# 策略源码。
- `README.md`：英文说明。
- `README_cn.md`：中文说明（当前文件）。
- `README_ru.md`：俄文说明。
