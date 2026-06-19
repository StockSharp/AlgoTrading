# Exp XHullTrend Digit 策略

## 概览
- 将 `MQL/22117` 下的 MQL5 专家 `Exp_XHullTrend_Digit.mq5` 转换为 StockSharp 高层 API 策略。
- 自定义 `XHullTrendDigitIndicator` 指标复刻原始 XHullTrend Digit 的算法与取整机制。
- 默认在 8 小时周期上工作，可根据需要调整到其他 K 线周期来跟踪中期趋势。

## 指标逻辑
1. 从所选的蜡烛价格（默认收盘价）取输入数据。
2. 使用指定平滑方式分别计算长度为 `BaseLength` 与 `BaseLength / 2` 的两条移动平均线。
3. 通过 `2 * 快线 - 慢线` 得到 Hull 风格的预测值，并依次用 `SignalLength` 与 `sqrt(BaseLength)` 再平滑两次。
4. 两条结果线都会按合约最小跳动 * `10^RoundingDigits` 的粒度四舍五入，复现 MQL5 版本的 Digit 取整效果。
5. 若取整后两条线相等但原始值不同，会沿着原始差值方向把快线或慢线微调一个跳动，以保证交叉仍然可见。

## 交易规则
- 仅在收盘的 K 线上进行信号判断。
- `SignalBar` 指定用于检测交叉的历史偏移（1 表示比较上一根与再上一根 K 线）。
- 做多条件：上一根快线在慢线上方，且目标偏移处快线小于或等于慢线（向上穿越）；可选地同时平掉空头。
- 做空条件：上一根快线在慢线下方，且目标偏移处快线大于或等于慢线（向下穿越）；可选地同时平掉多头。
- 多头离场：上一根快线跌破慢线。
- 空头离场：上一根快线上穿慢线。
- 如果出现反向信号且持有相反仓位，会先发出平仓单，再发送翻向所需的市价单。

## 参数
- `OrderVolume` – 每次进场的手数/数量。
- `StopLoss` / `TakeProfit` – 以价格跳动数表示的止损和止盈距离，通过 `UnitTypes.Step` 应用。
- `EnableBuyEntry`, `EnableSellEntry` – 控制是否允许开多或开空。
- `EnableBuyExit`, `EnableSellExit` – 控制是否自动平多或平空。
- `CandleType` – 指标所用的 K 线周期（默认 8 小时）。
- `BaseLength` – 指标的基础平滑长度，对应 MQL5 版本的 `XLength`。
- `SignalLength` – Hull 平滑长度，对应 MQL5 的 `HLength`。
- `PriceSource` – 参与计算的蜡烛价格（收盘/开盘/最高/最低/典型价/加权价/中值/均价）。
- `SmoothMethod` – 各阶段使用的均线类型（简单、指数、平滑、加权）。
- `Phase` – 与原脚本兼容的参数，对当前实现的平滑方式没有直接作用。
- `RoundingDigits` – 参与 Digit 取整的附加位数。
- `SignalBar` – 信号评估的偏移量（0 代表当前收盘，1 代表上一根等）。

## 风险控制
- 通过 `StartProtection` 把止损和止盈转换成跳动数进行保护，可按需要关闭或调整。
- 通过 `OrderVolume` 设定下单数量，以适配不同合约规模。

## 备注
- 指标依赖 `Security.PriceStep` 来计算跳动，请确保交易品种的最小变动价位已正确设置。
- 目前实现了 SMA、EMA、SMMA (RMA) 与 LWMA 四种平滑，与原脚本一致的其他高级平滑可在后续按需补充。
- 适用于任何提供所选周期 K 线的市场品种，切换资产时建议同步调整平滑长度与取整位数以保持敏感度。
