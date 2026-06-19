# Fractal MFI 策略

该策略翻译自 `Exp_Fractal_MFI.mq5`。它利用资金流量指数（MFI）在振荡器突破预设上、下阈值时生成交易信号。

## 工作原理
- 按照可配置周期计算 MFI。
- 当上一周期的 MFI 高于**低阈值**且当前值跌破该阈值时触发信号。
  - **Direct** 模式下开多单并可选关闭空单。
  - **Against** 模式下开空单并可选关闭多单。
- 当上一周期的 MFI 低于**高阈值**且当前值突破该阈值时触发另一个信号。
  - **Direct** 模式下开空单并可选关闭多单。
  - **Against** 模式下开多单并可选关闭空单。

策略只处理完成的K线，并可分别启用或禁用多单/空单的开仓与平仓。

## 参数
- `MfiPeriod` – MFI 的计算周期。
- `HighLevel` – MFI 的上阈值。
- `LowLevel` – MFI 的下阈值。
- `CandleType` – 用于计算的K线时间框。
- `Trend` – 选择 `Direct` 顺势交易或 `Against` 反向交易。
- `BuyPosOpen` / `SellPosOpen` – 允许开多或开空。
- `BuyPosClose` / `SellPosClose` – 允许在反向信号出现时平仓。

## 备注
该 C# 版本专注于高层 API 的使用，未实现原 MQL 代码中的资金管理和止损设置。
