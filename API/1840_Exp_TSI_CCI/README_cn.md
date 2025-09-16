# Exp TSI CCI 策略

该策略基于商品通道指数 (CCI) 计算真实强度指数 (TSI)，并根据与信号线的交叉进行交易。

## 逻辑
- 使用指定周期计算 CCI。
- 将 CCI 数值传入 TSI 指标，设置短期和长期平滑长度。
- 对 TSI 结果应用 EMA 以生成信号线。
- 当 TSI 上穿信号线时做多。
- 当 TSI 下穿信号线时做空。

## 参数
- `Candle Type` – 使用的K线周期。
- `CCI Period` – CCI 计算周期。
- `TSI Short Length` – TSI 短期平滑长度。
- `TSI Long Length` – TSI 长期平滑长度。
- `Signal Length` – TSI信号线的EMA长度。

## 指标
- Commodity Channel Index
- True Strength Index
- Exponential Moving Average

## 免责声明
本策略仅用于教育目的，不构成投资建议。
