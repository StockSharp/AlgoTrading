# KWAN RDP 趋势策略

该策略是 MetaTrader 专家顾问 `Exp_KWAN_RDP` 的 StockSharp 版本。它通过组合三种指标并进行平滑来计算 KWAN RDP 振荡器：

1. **DeMarker**：比较最近高点和低点，用来衡量动量耗尽程度。
2. **资金流量指数（MFI）**：结合价格与成交量判断超买或超卖。
3. **动量（Momentum）**：衡量价格变化速度。
4. 将原始值 `100 * DeMarker * MFI / Momentum` 通过可配置的移动平均（SMA、EMA、SMMA、WMA 或 Jurik）进行平滑。

平滑后的振荡器斜率用于产生信号：

- **斜率向上**：平掉空头仓位，并在允许的情况下开多。
- **斜率向下**：平掉多头仓位，并在允许的情况下开空。
- 斜率为零时不执行任何操作。

## 参数

- `CandleType`：用于计算指标的K线类型（默认 H1）。
- `DeMarkerPeriod`：DeMarker 指标周期。
- `MfiPeriod`：资金流量指数周期。
- `MomentumPeriod`：动量指标周期。
- `SmoothingLength`：平滑移动平均的周期长度。
- `Smoothing`：平滑方法（Simple、Exponential、Smoothed、Weighted、Jurik）。
- `EnableLongEntries` / `EnableShortEntries`：允许开多或开空。
- `CloseLongsOnReverse` / `CloseShortsOnReverse`：在反向信号出现时是否平仓。
- `TakeProfitPercent` / `StopLossPercent`：可选的百分比止盈止损，通过 `StartProtection` 应用。

## 交易规则

1. 订阅指定周期的K线，并在每根完成的K线上计算 DeMarker、MFI、Momentum 及平滑后的 KWAN 值。
2. 判断最新振荡器数值与前一数值之间的斜率方向。
3. 当斜率向上时，先平掉空头仓位（若启用），若允许做多且当前无多头，则以市价做多。
4. 当斜率向下时，先平掉多头仓位（若启用），若允许做空且当前无空头，则以市价做空。
5. 通过可选的百分比止盈止损保护持仓。

## 说明

- 仅在收盘K线处理信号，避免盘中噪声。
- DeMarker 的计算包含内部平滑，以匹配原始 MQL 实现。
- 按项目要求，C# 代码中的注释全部为英文。
