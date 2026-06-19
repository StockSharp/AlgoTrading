# Kloss MQL/8186 策略
[English](README.md) | [Русский](README_ru.md)

**Kloss MQL/8186 策略** 是 MetaTrader 4 专家顾问 `Kloss.mq4` 的 StockSharp 移植版本。策略结合 CCI 指标、随机振荡器以及向后平移的典型价格过滤器，通过单一仓位的方式完成反转。该实现保留了原始入场阈值、止损与止盈距离以及固定手数/资金百分比的仓位管理逻辑，并使用 StockSharp 的高级蜡烛订阅 API。

## 交易逻辑

- **数据**：使用所选时间框架的已完成蜡烛（默认 5 分钟），所有指标均基于同一时间序列。
- **指标**：
  - 周期为 10 的 CCI，比较其绝对值与 `±CciThreshold`（默认 120）。
  - 随机振荡器，参数 `%K=5`、`%D=3`、平滑系数 `=3`，检测 %K 主线是否进入超买或超卖区域。
  - 典型价格 ((High + Low + Close) / 3)，向后平移五根完成蜡烛，以复刻 EA 中带有偏移的 LWMA 过滤器。
- **做多条件**：
  - CCI ≤ `-CciThreshold`。
  - 随机振荡器 %K < `StochasticOversold`（默认 30）。
  - 前一根蜡烛的开盘价 > 五根之前的典型价格。
  - 当前没有多头持仓（`Position <= 0`）。若存在空头，将在单次市价单中同时平仓并建立多头。
- **做空条件**：
  - CCI ≥ `CciThreshold`。
  - 随机振荡器 %K > `StochasticOverbought`（默认 70）。
  - 前一根蜡烛的收盘价 < 五根之前的典型价格。
  - 当前没有空头持仓（`Position >= 0`）。若存在多头，将在单次市价单中翻转为空头。
- **持仓管理**：调用 `StartProtection` 自动根据点数距离放置止损与止盈。策略始终保持至多一个仓位（空、平、或多）。

## 仓位大小

- **固定手数**：当 `FixedVolume > 0` 时，始终以该数量下单（会按照 `VolumeStep` 和 `MinVolume` 调整）。
  
- **资金百分比**：当 `FixedVolume = 0` 时，按照账户权益的 `RiskPercent`（默认 0.2）除以最新收盘价估算下单量，并通过 `MaxVolume`（默认 5）限制并根据成交量步长取整。
- **安全措施**：如果账户信息缺失或计算结果 ≤ 0，则退回到最小可交易量。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `CciPeriod` | 计算 CCI 时使用的蜡烛数量。 | 10 |
| `CciThreshold` | 触发信号的 CCI 绝对阈值。 | 120 |
| `StochasticKPeriod` | 随机振荡器 %K 周期。 | 5 |
| `StochasticDPeriod` | 随机振荡器 %D 平滑周期。 | 3 |
| `StochasticSmooth` | %K 额外平滑参数。 | 3 |
| `StochasticOversold` | 确认做多的 %K 阈值。 | 30 |
| `StochasticOverbought` | 确认做空的 %K 阈值。 | 70 |
| `StopLossPoints` | 止损距离（点）。 | 48 |
| `TakeProfitPoints` | 止盈距离（点）。 | 152 |
| `FixedVolume` | 大于零时使用的固定下单量。 | 0 |
| `RiskPercent` | 当 `FixedVolume = 0` 时按账户权益换算的比例。 | 0.2 |
| `MaxVolume` | 最大允许下单量。 | 5 |
| `CandleType` | 指标计算使用的蜡烛类型/时间框架。 | 5 分钟蜡烛 |

## 执行注意事项

- **单一仓位**：策略不会加仓或分批，信号出现时直接用一笔市价单翻转方向。
- **指标同步**：典型价格过滤器需要最近五根完成蜡烛，因此至少处理六根蜡烛后才可能触发首笔交易。
- **止损止盈**：`StartProtection` 会用 `PriceStep` 将点值转换为绝对价格；若无法获取步长，则直接使用点值。
- **数据需求**：需要 OHLC 蜡烛数据；下单量会遵循 `MinVolume` 和 `VolumeStep` 设置。
- **与 MT4 差异**：自由保证金的计算以账户权益 (`Portfolio.CurrentValue`) 近似，若权益不可用则退回最小下单量。

## 使用建议

1. 根据在 MT4 中使用的时间框架设置 `CandleType`（原策略为 M5）。
2. 结合品种的最小跳动单位检查止损和止盈距离，必要时调整参数。
3. 若需固定手数，将 `FixedVolume` 设为目标数量并把 `RiskPercent` 设为 0。
4. 在新的标的上部署前，可开启参数优化以重新校准指标阈值。

