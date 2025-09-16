# Up3x1 策略（MT5 版本转换）

## 概述
- 将 MetaTrader 5 专家顾问 `up3x1.mq5` 转换为 StockSharp 高层 API 策略实现。
- 采用三重指数移动平均线（EMA）交叉信号，并带有止损、止盈和移动止损管理。
- 仅在 K 线收盘后处理数据，以模拟原始脚本中 `iTickVolume(0) > 1` 的判断（每根柱子只处理一次）。
- 默认使用 1 小时 K 线，可通过 `CandleType` 参数自定义时间框。

## 交易逻辑
1. **指标配置**
   - 快速 EMA（`FastPeriod`，默认 24）。
   - 中速 EMA（`MediumPeriod`，默认 60）。
   - 慢速 EMA（`SlowPeriod`，默认 120）。
2. **做多条件**
   - 上一根柱子：快速 EMA 低于中速 EMA，且中速 EMA 低于慢速 EMA。
   - 当前柱子：中速 EMA 上穿快速 EMA，同时快速 EMA 仍低于慢速 EMA。
3. **做空条件**
   - 上一根柱子：快速 EMA 高于中速 EMA，且中速 EMA 高于慢速 EMA。
   - 当前柱子：中速 EMA 上穿快速 EMA，同时二者仍高于慢速 EMA。
4. **平仓规则（多空共用）**
   - 价格相对入场价前进 `TakeProfitOffset` 即止盈（多单用最高价，空单用最低价判断）。
   - 价格相对入场价回撤 `StopLossOffset` 即止损（多单看最低价，空单看最高价）。
   - 当浮动盈利超过 `TrailingStopOffset` 后启用移动止损，按照固定距离跟随价格（使用柱状图高低价评估）。
   - 若快速 EMA 再次下穿中速 EMA 且二者保持在慢速 EMA 上方，则强制平仓（复刻 MQL 代码中的 `ma_one_1 > ma_two_1 > ma_three_1` 判断）。

## 仓位与风险管理
- `RiskFraction`（默认 0.02）与当前账户净值相乘，近似原脚本 `FreeMargin * 0.02 / 1000` 的动态手数公式。
- `BaseVolume`（默认 0.1）在无法取得组合价值或计算结果非正时作为备用手数。
- 当连续亏损次数超过 1 次后，仓位将按 `volume * losses / 3` 的比例减少；`losses` 计数在盈利后不会清零，与原脚本保持一致。
- 成交量会向下对齐至 `Security.VolumeStep`，并受 `Security.MinVolume` 与 `Security.MaxVolume` 约束；若无法满足最小手数则放弃下单。

## 参数列表
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `FastPeriod` | 24 | 快速 EMA 周期。
| `MediumPeriod` | 60 | 中速 EMA 周期。
| `SlowPeriod` | 120 | 慢速 EMA 周期，用于趋势过滤。
| `TakeProfitOffset` | 0.015 | 止盈距离（绝对价格差，需根据品种报价调整）。
| `StopLossOffset` | 0.01 | 止损距离（绝对价格差）。
| `TrailingStopOffset` | 0.004 | 移动止损距离，设置为 0 可禁用。
| `BaseVolume` | 0.1 | 动态手数失效时的备用仓位大小。
| `RiskFraction` | 0.02 | 用于动态仓位计算的账户价值比例。
| `CandleType` | 1 小时 | 指标计算与信号触发所用的 K 线类型。

## 转换说明
- 由于高层 API 按收盘柱处理数据，移动止损与保护性出场基于 K 线最高/最低价评估，而非逐笔报价，以便在回测与实盘中保持一致性。
- 止损与止盈通过市价平仓执行，而不是挂出保护性委托，从而与高层策略框架兼容。
- 动态仓位依赖 `Portfolio.CurrentValue`，若该数据不可用则退回 `BaseVolume`，类似原脚本 `LotCheck` 回退到手工输入手数。
- `losses` 计数不会因盈利而清零，完全沿用 MQL 脚本的累积逻辑。
- 所有代码注释均使用英文，以符合项目规范。

## 使用建议
1. 将策略绑定到目标证券和投资组合，并设置 `CandleType` 以匹配在 MT5 中使用的时间周期。
2. 根据标的报价精度调整止盈/止损/移动止损的绝对距离（例如 5 位报价的外汇品种中 0.015 ≈ 150 点）。
3. 结合账户规模校准 `RiskFraction` 与 `BaseVolume`，确保头寸大小合理。
4. 如需关闭移动止损，可将 `TrailingStopOffset` 设为 0。
5. 日志中会输出 “Enter long”“Exit short” 等信息，对应原脚本的 `Print` 调试输出。

## 目录结构
```
API/2512_Up3x1/
├── CS/Up3x1Strategy.cs      # 转换后的 C# 策略
├── README.md                # 英文说明
├── README_cn.md             # 中文说明（本文件）
└── README_ru.md             # 俄文说明
```

## 免责声明
量化策略交易具有高风险。本示例仅供学习研究，请在历史数据与模拟环境中充分验证后再考虑实际部署。
