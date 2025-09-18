# Exp TrendMagic 策略

## 概述
Exp TrendMagic 策略是 MetaTrader 5 智能交易系统 “Exp_TrendMagic” 的等价移植版本。策略持续监控 TrendMagic 指标的颜色切换，该指标由 CCI（商品通道指数）与 ATR（平均真实波幅）通道组合而成。当颜色发生转变时，系统会平掉相反方向的持仓，并在允许的情况下按新趋势方向开仓。

移植版本完全保留了原始 EA 的资金管理选项、信号偏移 (`Signal Bar`) 设置以及多种多空开/平仓权限开关。

## 交易逻辑
1. **指标输入**
   - `CCI`：可配置周期与价格源。
   - `ATR`：可配置周期，用于估算波动幅度。
   - TrendMagic 计算方式：
     - 当 CCI ≥ 0：`TrendMagic = Low - ATR`，并保持支撑线不会向下折返。
     - 当 CCI < 0：`TrendMagic = High + ATR`，并保持阻力线不会向上抬升。
   - 线段颜色编码：**0** 代表看涨（支撑位于价格下方），**1** 代表看跌（阻力位于价格上方）。

2. **信号判定**
   - 策略将颜色序列存入缓冲区，以复现 MetaTrader 指标缓存的行为，并通过 `Signal Bar` 偏移读取最近完成的 K 线信号。
   - 若上一根颜色（`Signal Bar + 1`）为 **0**，而当前颜色（`Signal Bar`）为 **1**，则视为趋势由空转多：先平掉空头，再按权限开多单。
   - 若上一根颜色为 **1**，当前颜色为 **0**，则视为趋势由多转空：先平掉多头，再按权限开空单。
   - `Allow Buy/Sell Entry` 与 `Allow Buy/Sell Exit` 四个开关严格对应原 EA 的开仓/平仓许可。

3. **资金管理**
   - `Money Management` 控制每次交易使用的资金比例。若取负值，则被视作固定手数。
   - `Margin Mode` 指定资金管理值的解释方式：
     - `FreeMargin` / `Balance`：按账户权益的百分比下单，再除以价格得到手数。
     - `LossFreeMargin` / `LossBalance`：按账户权益的百分比衡量可承受亏损，再除以止损距离得到手数。
     - `Lot`：将参数直接视为固定下单量。
   - 计算得到的手数会自动贴合交易品种的 `VolumeStep`、`MinVolume` 与 `MaxVolume`。

4. **风控机制**
   - 新仓位建立后，记录开仓价并按照点数（`PriceStep` 的倍数）执行与 MT5 相同的止损、止盈逻辑。
   - 触发止损或止盈时立即平仓，并清空记录的入场价格。
   - 内置的时间节流装置禁止在下一根 K 线到来之前重复开同方向的仓位，从而复刻 MT5 中的 “时间限制” 检查。

## 参数说明
| 参数 | 说明 |
|------|------|
| `Money Management` | 交易资金占比（负值表示固定手数）。 |
| `Margin Mode` | 资金管理的计算方式。 |
| `Stop Loss` | 止损距离（点数）。 |
| `Take Profit` | 止盈距离（点数）。 |
| `Deviation` | 与 MT5 输入保持一致的滑点占位参数。 |
| `Allow Buy/Sell Entry` | 控制多/空开仓许可。 |
| `Allow Buy/Sell Exit` | 控制平空/平多许可。 |
| `Candle Type` | 指标与信号所用的主时间框。 |
| `CCI Period` / `CCI Price` | CCI 周期与应用价格。 |
| `ATR Period` | ATR 周期。 |
| `Signal Bar` | 读取信号的已完成 K 线索引。 |

## 其他说明
- 策略只处理已经完成的 K 线（`CandleStates.Finished`），以保持与原 MT5 按 tick 驱动的逻辑一致。
- 每次启动或回测重置时，所有指标与内部状态都会清空，便于获得确定性的优化结果。
- `Deviation` 参数在 StockSharp 中不会直接作用于市价单，只是为了与原策略界面保持一致。
