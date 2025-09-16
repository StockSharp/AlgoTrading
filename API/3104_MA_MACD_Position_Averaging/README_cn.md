# MA MACD Position Averaging

该策略忠实移植自 MetaTrader 专家顾问 **“MA MACD Position averaging”**。它使用加权移动平均线与 MACD 比值过滤信号，并在行情对持仓不利时按设定的点数间隔执行“金字塔式”加仓。所有风险参数均以点（pip）为单位配置，并根据 StockSharp 提供的品种属性自动换算为价格偏移量。

## 交易逻辑

1. **指标准备**
   - 在每根完成的 K 线上计算可配置的移动平均线（`MaPeriod`、`MaMethod`、`MaAppliedPrice`）。`SignalBar` 与 `MaShift` 参数复刻了 MetaTrader 读取历史柱线和水平偏移指标的能力。
   - 同时计算 MACD（`MacdFastPeriod`、`MacdSlowPeriod`、`MacdSignalPeriod`、`MacdAppliedPrice`），并把主线与信号线值存入循环缓冲，以便随时读取历史值而无需直接访问指标接口。
2. **入场条件**
   - **做多**：MACD 主线与信号线均小于零，`MACDmain / MACDsignal` 比值不低于 `MacdRatio`，收盘价位于移动平均线上方，并且价格与均线的距离不少于 `IndentPips` 点。
   - **做空**：MACD 两条线均大于零，比值高于 `MacdRatio`，收盘价低于均线且距离不少于 `IndentPips` 点。
   - 只有在当前无持仓时才会触发新的方向性入场；一旦进入加仓循环，就只执行平均加仓规则。
3. **加仓模块**
   - 若存在多头腿且价格自最佳（最低）买入价下跌至少 `StepLossingPips` 点，则按“上一腿成交量 × `LotCoefficient`”的手数再买入一笔（按照品种的最小交易步长向下取整）。
   - 若存在空头腿且价格自最佳（最高）卖出价上升至少 `StepLossingPips` 点，则按相同公式追加空头。
   - 如果意外出现多空同时存在的情况，策略会立即平掉所有腿以保持状态一致。
4. **风控与出场**
   - 每条腿拥有独立的止损/止盈水平（`StopLossPips`、`TakeProfitPips`）。在每根完成的 K 线上检测蜡烛最高价/最低价是否触及这些水平，若触及则通过市价单平仓。
   - 可选的追踪止损（`TrailingStopPips`、`TrailingStepPips`）在浮盈超过 `TrailingStopPips + TrailingStepPips` 点后启动，并将止损向盈利方向移动 `TrailingStopPips` 点；只有在价格继续前进至少 `TrailingStepPips` 点时才会再次移动。
5. **辅助规则**
   - 所有下单量都会按合约的最小成交单位对齐，并限制在允许的最小/最大范围内。策略仅在 `CandleStates.Finished` 状态下处理数据，避免重复触发。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 指标计算所用的时间框架。 |
| `OrderVolume` | `decimal` | `0.1` | 首次入场的基础手数。 |
| `StopLossPips` | `int` | `50` | 止损距离（点），0 表示禁用。 |
| `TakeProfitPips` | `int` | `50` | 止盈距离（点），0 表示禁用。 |
| `TrailingStopPips` | `int` | `5` | 追踪止损与价格之间的距离（点）。启用追踪时必须大于 0。 |
| `TrailingStepPips` | `int` | `5` | 追踪止损每次前移所需的额外利润（点）。 |
| `StepLossingPips` | `int` | `30` | 触发加仓所需的反向波动幅度（点）。 |
| `LotCoefficient` | `decimal` | `2.0` | 每次加仓的成交量系数。 |
| `SignalBar` | `int` | `0` | 指标采样向前回看的完成柱数量。 |
| `MaPeriod` | `int` | `15` | 移动平均线长度。 |
| `MaShift` | `int` | `0` | 移动平均线的水平偏移（柱数）。 |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | 移动平均的平滑方式（简单、指数、平滑、加权）。 |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | 输入到移动平均线的价格类型。 |
| `IndentPips` | `int` | `4` | 入场时价格与均线的最小间隔（点）。 |
| `MacdFastPeriod` | `int` | `12` | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | `int` | `26` | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | `int` | `9` | MACD 信号线周期。 |
| `MacdAppliedPrice` | `AppliedPriceType` | `Weighted` | MACD 使用的价格类型。 |
| `MacdRatio` | `decimal` | `0.9` | MACD 主线/信号线的最低允许比值。 |

### 点值换算

`StopLossPips`、`TakeProfitPips`、`TrailingStopPips`、`TrailingStepPips`、`StepLossingPips`、`IndentPips` 等参数会乘以品种的 `PriceStep`。若报价保留 3 或 5 位小数，则额外乘以 10 以匹配 MetaTrader 对“pip”的定义；若缺少步长信息，则使用 0.0001 作为默认值。

## 实现细节

- 由于 StockSharp 使用净持仓模式，策略在内部维护一个腿列表，每条腿都记录入场价、止损和止盈，以复现 MetaTrader 中逐单管理的行为。
- 止损/止盈通过软件模拟实现：当蜡烛触碰到相应水平时，在同一根蜡烛上用市价单平仓。
- 当 `StepLossingPips` 为 0 时自动停用加仓逻辑；否则新腿的手数等于上一腿手数乘以 `LotCoefficient`，并按交易单位向下取整。
- 追踪止损以蜡烛收盘价作为当前价格参考，只会向盈利方向移动，且必须等到收益超过 `TrailingStopPips + TrailingStepPips` 点才会启动。
- 指标缓冲遵循 `SignalBar` 与 `MaShift` 设置，因此决策所使用的数据与原始 MetaTrader 指标缓冲保持一致。
