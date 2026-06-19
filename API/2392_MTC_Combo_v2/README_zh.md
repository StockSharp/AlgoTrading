# MTC Combo v2 策略

来自 MetaTrader 脚本“MTC Combo v2 (barabashkakvn's edition)”的转换。

## 逻辑
- 通过移动平均线斜率判断趋势。
- 感知器过滤器计算多个间隔的开盘价差的加权和。
- `Pass` 选择使用的分支：
  - 4：`perceptron3 > 0` 且 `perceptron2 > 0` 时做多；`perceptron3 <= 0` 且 `perceptron1 < 0` 时做空。
  - 3：`perceptron2 > 0` 时做多。
  - 2：`perceptron1 < 0` 时做空。
  - 其他：仅依据 MA 斜率交易。

止损与止盈由 `Sl*`、`Tp*` 参数给出。

## 参数
- `MaPeriod` – 移动平均周期。
- `P2`、`P3`、`P4` – 感知器所用的间隔。
- `Pass` – 决策模式。
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3` – 各分支的止损与止盈。
- `CandleType` – 处理的蜡烛类型。

## 说明
策略一次只持有一笔仓位，满足止损或止盈条件后平仓。

## 免责声明
仅供学习，不构成投资建议。
