# PowerZone 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略识别“PowerZone”订单块：先出现一根看跌K线，随后连续出现若干根看涨K线（反之亦然）。价格向上突破牛区高点买入，向下跌破熊区低点卖出。止盈与止损根据区间大小计算。

## 细节

- **入场条件**：
  - **多头**：`Periods+1`根之前的看跌K线，之后`Periods`根看涨K线，并且价格突破区间上沿。
  - **空头**：`Periods+1`根之前的看涨K线，之后`Periods`根看跌K线，并且价格跌破区间下沿。
- **多空方向**：双向。
- **出场条件**：止盈和止损为区间宽度的倍数。
- **指标**：无。
- **默认值**：
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **过滤器**：
  - 类别：突破
  - 方向：双向
  - 指标：无
  - 止损：有
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

## 计算与执行

C# 和 Python 使用已完成K线。在决策K线 `t`，基准K线为 `t - Periods - 1`，其后至 `t - 1` 的 `Periods` 根K线必须全部严格收涨才形成牛区，或全部严格收跌才形成熊区。基准K线颜色严格相反，十字星不合格。当前决策K线不参与确认，因此不会在确认完成前使用区域。

要求 `100 * abs(Close[t - 1] - 基准 Close) / abs(基准 Close) >= Threshold`，基准收盘价不能为零。区域只取基准K线的实体 `[min(Open, Close), max(Open, Close)]`，`UseWicks=true` 时取其 `[Low, High]`。确认K线的极值不扩大区域，区域宽度必须为正。分别保留最近牛区和熊区直到替换，每个区域最多接受一次入场。

做多要求当前 `Close > 牛区高点`，做空要求 `Close < 熊区低点`，同价保持仓位。新确认区域在第一个决策K线即可入场，即使价格已经越过边界。空仓入场 `Volume`，反向仓位用 `Volume + abs(Position)` 完全反转，不在同一方向加仓。空仓时两方向同时合格则优先做多。有未完成订单时不再下单，没有 EMA、冷却期或其他指标。

接受入场时固定 `区域宽度 * StopLossFactor` 和 `区域宽度 * TakeProfitFactor`，以实际成交价为基准；同侧部分增仓使用成交量加权均价，减仓保留基准，反转重置。后续区域不改变已有仓位的保护距离。本地保护检查多仓可执行 bid、空仓 ask 和已完成K线收盘价，用市价单平掉全部剩余仓位，同一K线上优先于新入场。不把最高/最低价触及视为盘中成交；跳空可导致实际成交价不同。

上述默认值两语言相同：API 参数 `TakeProfitFactor=1.5`、`StopLossFactor=1`、`CandleType` 为五分钟。已发布参数名为 `UseWicks`，C# 属性名为 `IsUseWicks`。`Periods` 和倍数必须为正，`Threshold` 是非负百分比。这些定义明确了原简介未规定的阈值、边界、一次性使用和成交基准规则。
