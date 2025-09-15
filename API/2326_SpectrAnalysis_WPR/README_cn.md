# SpectrAnalysis WPR 策略

该策略由 MQL5 专家顾问 *Exp_i-SpectrAnalysis_WPR* 转换而来。
策略通过监控 Williams %R 指标方向的变化来开平仓。

## 逻辑

1. 订阅指定周期的K线。
2. 使用设定周期计算 Williams %R。
3. 保存最近两个指标值以判断上升或下降方向。
4. 当指标向上转折且允许做多时：
   - 如启用则平掉空头头寸。
   - 开立新的多头头寸。
5. 当指标向下转折且允许做空时：
   - 如启用则平掉多头头寸。
   - 开立新的空头头寸。

策略只处理已完成的K线，不使用复杂的历史查询，并依赖高级 API 绑定。

## 参数

| 名称 | 描述 | 默认值 |
| --- | --- | --- |
| `Candle Type` | 用于计算的K线周期 | `4h` |
| `WPR Period` | Williams %R 指标周期 | `13` |
| `Allow Long Entry` | 允许开多 | `true` |
| `Allow Short Entry` | 允许开空 | `true` |
| `Allow Long Exit` | 允许平多 | `true` |
| `Allow Short Exit` | 允许平空 | `true` |

## 说明

原始 MQL 版本对 Williams %R 输出进行谱分析。
此 C# 转换使用标准 Williams %R 指标，并通过跟踪最近的指标值复制信号逻辑。
