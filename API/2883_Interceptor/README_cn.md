# Interceptor 策略（StockSharp 版本）

## 概述
Interceptor 策略是 MetaTrader5 专家顾问的 C# 移植版。策略在 GBP/USD 5 分钟图上同时观察 M5、M15、H1 三个周期的 EMA "扇形" 结构，并结合 Stochastic 指标、盘整突破、锤子线过滤、指标背离及扇形收敛（Horn）条件来确认趋势延续信号。

## 核心逻辑
- **趋势结构**：在 M5/M15/H1 上分别计算 34/55/89/144/233 周期 EMA，只有当扇形完全按多头或空头顺序排列且 EMA 之间的最大距离小于阈值时才认为趋势有效。
- **动能确认**：M5 与 M15 的 Stochastic 必须自超卖/超买区穿越，以证明价格已摆脱盘整区。
- **盘整突破**：通过 `FlatnessCoefficient`、`MinFlatBars`、`MaxFlatPoints` 控制的压缩识别算法寻找狭窄区间，当价格向上/向下突破时为信号加分。
- **锤子线过滤**：在配置的回溯条数内，若出现满足长影/短影比例要求且位于近期极值的锤子线或倒锤子线，则视为趋势方向的反转/延续提示。
- **Stochastic 背离**：检测价格与 M5 Stochastic 之间的高低点背离，以捕捉趋势方向的转折。
- **Horn 条件**：当 M5 EMA 扇形收敛并突破 `RangeBreakLookback` 定义的区间上沿/下沿时，在高周期趋势一致的前提下触发额外信号。

## 入场条件
多头信号可能由以下任一或多个条件触发（每满足一个条件即向信号列表添加理由）：
1. 三周期 EMA 扇形多头排列 + M5 Stochastic 多头交叉 + 当前 K 线实体大于 `MinBodyPoints`。
2. M5 扇形内的突破 K 线从最低价启动并收在所有快 EMA 之上。
3. 盘整区向上突破。
4. M5、M15 同时突破且 M15 扇形距离仍在限制之内。
5. 检测到 Stochastic 与价格的多头背离。
6. 回溯窗口内出现多头锤子线并位于区间低点。
7. M15 Stochastic 多头交叉并伴随连续阳线。
8. Horn 条件：扇形收敛后向上突破近端区间。

空头逻辑与多头对称。如果同一根 K 线上同时出现多头与空头信号，则不执行交易。

## 出场与风控
- 使用 `StopLossPoints` 与 `TakeProfitPoints` 设定初始止损、止盈。
- 当盈利达到 `TakeProfitAfterBreakeven` 后，可将止损移动到 `StopLossAfterBreakeven` 指定的距离，实现保本。
- `TrailingDistancePoints` 与 `TrailingStepPoints` 控制的移动止损会随价格推进。
- 新仓开立前会自动平掉相反方向持仓。

## 主要参数
- `Volume`：下单量。
- `FlatnessCoefficient` / `MinFlatBars` / `MaxFlatPoints`：盘整区检测控制参数。
- `StopLossPoints` / `TakeProfitPoints`：初始止损、止盈点数。
- `TakeProfitAfterBreakeven` / `StopLossAfterBreakeven`：触发保本与新的止损距离。
- `MaxFanDistanceM5/M15/H1`：各周期 EMA 扇形最大允许宽度。
- `StochasticKPeriod*`、`StochasticUpper*`、`StochasticLower*`：Stochastic 参数与超买/超卖阈值。
- `MinBodyPoints`：判定强势 K 线的最小实体。
- `MinDivergenceBars`：背离高/低点之间最少间隔。
- `Hammer*` 系列：锤子线判定的长度、百分比和回溯条数。
- `MaxFanWidthAtNarrowest` / `FanConvergedBars`：Horn 条件下扇形收敛的宽度和持续时间。
- `RangeBreakLookback`：区间突破的回溯长度。
- `TrailingStepPoints` / `TrailingDistancePoints`：移动止损配置。
- `CandleType`：主驱动 K 线数据类型（默认 5 分钟）。

## 使用提示
- 策略按原始 EA 设计，推荐用于 GBP/USD M5；更换品种或周期需重新调参。
- 必须同时订阅 M5、M15、H1 三个周期的时间 K 线数据。
- 策略只维护单向净头寸；开仓前会自动平掉相反仓位。

## 免责声明
策略仅用于研究与教学，历史表现不代表未来结果。在实盘使用前请充分回测并在模拟环境中验证参数与逻辑。
