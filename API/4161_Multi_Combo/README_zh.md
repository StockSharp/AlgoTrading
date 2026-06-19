# Multi Strategy Combo 策略

## 概述
**Multi Strategy Combo Strategy** 是 MetaTrader 4「Multi-Strategy iFSF」EA 的 C# 版本。该策略将 MA、RSI、MACD、Stochastic、Parabolic SAR 等指标的信号进行合成，并辅以 ADX 趋势过滤、布林带区间过滤以及噪音过滤器。所有逻辑均使用 StockSharp 的高阶 `SubscribeCandles().Bind(...)` API 完成，不再需要手动维护指标缓冲区。只有当所有启用的指标同时给出 BUY/SELL 信号时才会触发下单，随后再通过组合过滤器确认。

## 核心逻辑
* **信号合成** — 启用的 MA、RSI、MACD、Stochastic、SAR 分别给出离散多/空投票。所有投票一致时才判定为看多或看空。
* **组合因子（1–3）** — 对应原始 EA 的 `Combo_Trader_Factor`。不同因子决定共识信号与 ADX 趋势、布林带过滤器之间的组合方式：
  * *因子 1* 偏向趋势行情；在区间阶段（且启用布林带过滤器）时使用布林带反弹信号。
  * *因子 2* 要求更严格，趋势过滤与布林带过滤都必须支持当前共识。
  * *因子 3* 最严格，只有在全部模块方向一致时才允许交易。
* **趋势检测** — 在可配置的时间框架上计算 ADX，将市场划分为趋势上涨/趋势下跌/区间看涨/区间看跌。
* **布林带过滤** — 使用 2σ 与 3σ 两组带宽。做多必须出现下轨反弹且近期 RSI 进入超卖区域；做空则相反。
* **噪音过滤** — 以 ATR 模拟 Damiani Volatmeter，当波动率低于阈值时禁止新交易。
* **自动平仓** — 若启用，在共识出现反向信号时立即平掉当前仓位。

## 指标与信号
* **移动平均线** — 三条可配置均线（周期与算法）。模式 1–5 对应原策略的不同交叉组合。
* **RSI** — 四种模式：超买/超卖、趋势、组合以及区间模式，相关阈值均可配置。
* **MACD** — 四种模式：主线/信号线斜率、零轴附近交叉、组合确认以及信号线穿越零轴。
* **随机指标** — 可选择简单的 %K/%D 交叉或带上限/下限的交叉。
* **Parabolic SAR** — 可选的方向投票，可保持最后一次方向以避免同一趋势内重复触发。

## 风险管理
* `StopLossOffset` 与 `TakeProfitOffset` 以绝对价差指定止损止盈距离。
* 通过 `StartProtection` 支持追踪止损 (`UseTrailingStop`)。
* 下单与持仓保护完全由 StockSharp 框架处理，无需手动 `OrderSend`。

## 关键参数
* **通用** — `ComboFactor`, `CandleType`。
* **MA** — `UseMa`, `MaMode`, 三条均线的周期与算法、时间框架以及“记住上一信号”选项。
* **RSI** — `UseRsi`, `RsiMode`, `RsiPeriod`, 超买/超卖阈值、区间阈值以及记忆开关。
* **MACD** — `UseMacd`, `MacdMode`, 快/慢/信号 EMA 周期、时间框架以及记忆开关。
* **随机指标** — `UseStochastic`, 平滑参数、阈值与时间框架。
* **Parabolic SAR** — `UseSar`, `SarStep`, `SarMax`, 时间框架。
* **趋势过滤** — `UseTrendDetection`, `AdxPeriod`, `AdxLevel`, 时间框架。
* **布林带过滤** — `UseBollingerFilter`, `BollingerPeriod`, 中/宽带偏差、RSI 历史长度。
* **噪音过滤** — `UseNoiseFilter`, `NoiseAtrLength`, `NoiseThreshold`, 时间框架。
* **自动平仓与风险** — `UseAutoClose`, `AllowOppositeAfterClose`, `StopLossOffset`, `TakeProfitOffset`, `UseTrailingStop`。

所有参数均通过 `StrategyParam<T>` 暴露，方便优化与 UI 显示。

## 与 MT4 EA 的差异
* 仅使用 StockSharp 内置指标。原策略中 ZeroLag MACD 的选项被统一成内置 MACD。
* 所有均线和振荡指标均基于 K 线收盘价，MT4 中的价格源与 Shift 参数（如 `FastMa_Price`, `FastMa_Shift`）未实现。
* Damiani 噪音过滤器改为 ATR 判定，可通过 `NoiseThreshold` 调整灵敏度。
* 不再手动管理订单，直接调用 `BuyMarket`/`SellMarket`，仓位保护由 `StartProtection` 完成。
* 不包含 MT4 的图形界面与文字注释，调试信息可使用 `LogInfo` 查看。

## 使用步骤
1. 将 `MultiStrategyComboStrategy` 类添加到 StockSharp 解决方案并编译。
2. 设置 `Security`、`Portfolio`、`Volume`，必要时为各指标选择不同时间框架。
3. 根据需求调整止损/止盈距离、追踪止损及过滤阈值。
4. 启动策略。只要所有启用模块依据所选组合因子达成一致，策略便会在 K 线收盘后下单。

## 转换说明
* 全部逻辑基于高阶订阅接口实现，无手工 `GetValue()` 调用。
* 源码缩进按照仓库要求使用制表符。
* 代码中的英文注释说明了 MT4 与 StockSharp 之间的映射关系。
