# Fisher Org v1 策略

该策略利用 Fisher 变换指标捕捉趋势反转。当指标形成局部低点时开多仓，形成局部高点时开空仓。出现反向信号时平掉当前持仓。

## 规则
- **多头**：`Fisher[t-2] > Fisher[t-1]` 且 `Fisher[t-1] <= Fisher[t]`
- **空头**：`Fisher[t-2] < Fisher[t-1]` 且 `Fisher[t-1] >= Fisher[t]`

## 参数
- `Fisher Length` – Fisher 变换周期（默认 7）
- `Candle Type` – 计算所用的K线时间框架

## 指标
- Fisher Transform
