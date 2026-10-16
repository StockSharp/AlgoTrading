# Dema RSI 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略交易双指数移动平均线的 RSI 与其平滑值的交叉。RSI 上穿平滑线时开多，下穿时开空。持仓可通过止盈、追踪止损和可选的交易时段过滤来保护。

## 参数
- K线类型
- MA 长度
- RSI 长度
- RSI 平滑长度
- 止盈点数
- 追踪止损点数
- 使用交易时段
- 会话开始时间
- 会话结束时间

### 实际 RSI(DEMA) 条件

默认：`CandleType=1m`、`MaLength=20`、`RsiLength=14`、`RsiSmoothingLength=5`、`TakeProfitPoints=0`、`TrailStopPoints=0`、`UseSession=false`、`SessionStart=09:00`、`SessionEnd=17:00`。对已完成K线计算 `EMA1=EMA(Close,20)`、`EMA2=EMA(EMA1,20)`、`DEMA=2×EMA1−EMA2`、`R=RSI(DEMA,14)`、`S=SMA(R,5)`。若**上一根** `R <= S` 且**当前** `R > S`，从空仓或空头转为做多；反向交叉 `上一根 R >= S` 且 `当前 R < S` 则做空，反转数量 `Volume + abs(Position)`。启用 `UseSession=true` 时入场限于 `[SessionStart,SessionEnd)`，可跨午夜，保护平仓不受限制。`TakeProfitPoints` 和 `TrailStopPoints` 单位为**证券最小价格刻度**；非零时使用原生绝对距离止盈与追踪止损，零表示关闭。原README未给出数值默认值，以上为实现选择。旧EMA120/450价格交叉与该策略无关。

