# Premarket Gap MomoTrader
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

只做多的蜡烛动量示例，每个纽约日历日最多入场一次。“跳空”指当前close相对前一close上涨，不要求隔夜开盘跳空或EMA交叉。

## 入场和仓位

只使用已完成的蜡烛，默认五分钟。第一根记录前一close和成交量。从空仓入场要求严格阳线、正的high-low范围、总成交量 > `MinVolume`、正的前一close，且 `100 * (close - previousClose) / previousClose >= MinGainPct`。

`bodyPercent = 100 * (close - open) / (high - low)`：≥90%使用策略`Volume`的100%；≥85%且<90%使用50%；≥75%且<85%使用25%；<75%不入场，边界包含。

若证券提供正的`VolumeStep`，数量向下取整至该步长。零数量或超出证券已提供最小/最大数量的订单被拒绝。不增加已有持仓。

`UseSession=true`时，提交时间必须在纽约`[SessionStart, SessionEnd)`内，默认04:00–09:30，考虑夏令时。禁用时段过滤仍保留每日一次限制。提交入场订单即用完该日机会，即使订单随后失败。

## 持有和退出

入场后，第一根阴线（`close < open`）或`volume <= previousVolume`的蜡烛完全平掉实际多头。阳线且成交量增加时继续持有；十字星在成交量增加时也持有。退出不受入场时段、涨幅、实体比例或最小成交量限制。退出蜡烛不同时再次入场。未完成订单阻止重复提交，但每根已完成蜡烛仍更新前一close和成交量。

没有指标、空头入场、六小时冷却、独立止损或止盈。下次入场须等到下一个符合条件的纽约日期。

## 默认值

| 参数 | 默认值 | 含义 |
|---|---:|---|
| `MinGainPct` | 5 | 包含边界的涨幅百分比下限，非负 |
| `MinVolume` | 15000 | 严格的蜡烛成交量下限，非负，使用数据源单位 |
| `UseSession` | true | 入场时段过滤；C#属性`IsUseSession` |
| `SessionStart` / `SessionEnd` | 04:00 / 09:30 | 纽约同一天内按顺序的时间，含起点、不含终点 |
| `CandleType` | 5分钟 | 已完成信号蜡烛 |

测试仅使用现有BTC/TON历史，检查每个订单和实际持仓、三个实体比例档位、数量步长、每日限制、两种退出、上涨持有和春季纽约夏令时变化。TON存在符合原始5%/15000设置的蜡烛；BTC机制测试明确调整阈值。不证明盈利。
