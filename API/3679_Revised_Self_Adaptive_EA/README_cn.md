# Revised Self Adaptive EA

MetaTrader 5 专家顾问 `revised_self_adaptive_ea.mq5` 的 StockSharp 高级 API 版本。

## 策略说明

* **形态识别**：检测最近两根已收盘 K 线是否形成吞没形态。看涨信号要求当前 K 线实体为阳线、开盘价低于前一根收盘价且前一根为阴线；看跌信号要求相反条件。实体大小与平均实体比较以过滤噪声。
* **动量过滤**：RSI 进入超卖区（看涨）或超买区（看跌）后才允许下单。
* **趋势过滤**：短周期简单移动平均必须与进场方向一致，防止逆势交易。
* **风控机制**：依据 ATR 计算止损、止盈与可选的移动止损，若价格触发保护位则立即市价平仓。
* **点差与风险限制**：当前买卖价差超出阈值或 ATR 止损对应的风险比例超过上限时，信号会被跳过。

## 主要参数

- `CandleType`：分析所用的 K 线周期（默认 1 小时）。
- `AverageBodyPeriod`：计算平均实体长度的样本数。
- `MovingAveragePeriod`：趋势过滤所用的 SMA 周期。
- `RsiPeriod`、`OversoldLevel`、`OverboughtLevel`：RSI 相关设定。
- `AtrPeriod`、`StopLossAtrMultiplier`、`TakeProfitAtrMultiplier`、`TrailingStopAtrMultiplier`、`UseTrailingStop`：ATR 风控相关配置。
- `MaxSpreadPoints`：允许的最大点差（以最小跳动单位表示）。
- `MaxRiskPercent`：基于 ATR 止损计算的最大可接受风险百分比。
- `TradeVolume`：下单手数。

## 与原始 MQL 的差异

- 原脚本只提供信号检测，本移植补充了移动平均确认以及 ATR 风控模块，以充分利用 MQL 中声明的指标。
- 在 StockSharp 中未实现箭头绘制，仅保留核心交易逻辑。
- 新增风险比例过滤，避免 ATR 止损过大的信号。

## 使用步骤

1. 在 Hydra 或自定义主机中指定投资组合与标的证券。
2. 根据交易计划设置参数，特别是 ATR 与点差限制。
3. 启动策略后系统将自动订阅 K 线、计算指标并在条件满足时发送市价单。
