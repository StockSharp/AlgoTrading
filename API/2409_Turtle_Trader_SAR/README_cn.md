[English](README.md) | [Русский](README_ru.md)

Turtle Trader SAR 将原始的 MQL5 Turtle 系统（可选 Parabolic SAR 追踪）转换为 StockSharp C#。
该策略利用 Donchian 通道突破进行交易，使用 ATR 计算风险并可对盈利头寸进行加仓。

## 工作原理

1. **指标计算**
   - 使用 20 周期 ATR 衡量波动性。
   - `ShortPeriod` 与 `ExitPeriod` 的 Donchian 通道。
   - 可选 Parabolic SAR 用于跟踪止损。
2. **头寸大小**
   - 每笔交易风险为账户权益的 `RiskFraction`。
   - 单位数量受 `MaxUnits` 限制。
3. **入场规则**
   - 收盘价高于 `ShortPeriod` 最高价 → 买入。
   - 收盘价低于 `ShortPeriod` 最低价 → 卖出。
4. **金字塔加仓**
   - 价格每向盈利方向移动 `AddInterval` 个 ATR，加仓一次，直至 `MaxUnits`。
5. **出场规则**
   - `ExitPeriod` 通道的反向突破。
   - 使用 `StopAtr` 的 ATR 止损及可选 `TakeAtr` 止盈。
   - 若 `UseSar=true`，则 Parabolic SAR 触发额外出场。

## 参数

- `ExitPeriod` = 10
- `ShortPeriod` = 20
- `LongPeriod` = 55
- `RiskFraction` = 0.01
- `MaxUnits` = 4
- `AddInterval` = 1
- `StopAtr` = 1
- `TakeAtr` = 1
- `UseSar` = false
- `SarStep` = 0.02
- `SarMax` = 0.2
- `CandleType` = 1 天

## 标签

- **类别**：趋势跟随
- **方向**：双向
- **指标**：ATR、Highest、Lowest、Parabolic SAR
- **止损**：ATR / SAR
- **复杂度**：中等
- **时间框架**：日线
- **季节性**：无
- **神经网络**：无
- **背离**：无
- **风险等级**：中等
