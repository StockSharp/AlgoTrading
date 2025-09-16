# ColorJFatl Digit NN3 MMRec 策略（StockSharp 版本）

本策略是 MetaTrader 5 专家顾问 *Exp_ColorJFatl_Digit_NN3_MMRec* 的 StockSharp 高级 API 迁移版。原始脚本依赖自定义的 ColorJFatl_Digit 指标和复杂的补仓管理。本版本保留信号引擎，将其拆分为三个在不同周期运行的模块。

每个模块都会把蜡烛价格转换为指定的价格类型（收盘价、典型价、DeMark 价等），然后送入 Jurik 移动平均 (JMA)。通过比较当前与上一个 JMA 值的差异判断斜率方向：斜率向上表示多头环境，模块会平掉空头并在允许时开多；斜率向下则执行相反操作。三个模块共享同一账户，因此始终处理净仓位。

## 交易流程

1. 订阅三个周期的蜡烛数据（默认：日线、8 小时、3 小时）。
2. 对每根已完成蜡烛执行：
   - 根据 *AppliedPrice* 选择价格输入。
   - 用 Jurik MA 平滑价格。
   - 计算当前与前一值的差，确定状态（上升、下降或保持不变）。
   - 根据 *SignalBar* 参数将状态放入队列，实现延迟信号。
3. 状态发生变化时：
   - **上升**：可选地平掉空头，按模块的成交量开多。
   - **下降**：可选地平掉多头，按模块的成交量开空。
4. 其他模块的信号可根据权限标志平仓或反向。

策略默认不设置固定止损/止盈，可结合 `StartProtection()` 或外部风控使用。

## 参数说明

每个模块（A、B、C）包含以下参数：

- **CandleType**：蜡烛时间周期。
- **JmaLength**：Jurik MA 周期。
- **JmaPhase**：保留原脚本的参数，StockSharp 的 JMA 暂不支持调整相位。
- **SignalBar**：执行信号前需要等待的已完成蜡烛数。
- **AppliedPrice**：价格类型，支持 Close、Open、Median、Typical、Weighted、Simple、Quarter、TrendFollow、DeMark 等。
- **AllowBuyOpen / AllowSellOpen**：允许开多 / 开空。
- **AllowBuyClose / AllowSellClose**：允许在反向信号时平多 / 平空。
- **Volume**：开仓数量。

由于共享净仓位，策略同一时刻只会维持一个方向的持仓。如果目标方向已有仓位则不会加仓；若方向相反则先平仓再视情况开仓。

## 使用建议

- `GetWorkingSecurities()` 会自动注册所需的所有蜡烛序列。
- 所有信号都在蜡烛收盘后触发，避免重绘。
- *AppliedPrice* 枚举完整复制了原指标的选项，包括两个 TrendFollow 价格和 DeMark 价格。
- 未移植 MQL 中的补仓算法，可通过调整 Volume 或 `StartProtection()` 控制风险。
- 代码中包含英文注释，方便维护及后续可能的 Python 版本移植。

## 扩展方向

- 若需要固定止损或止盈，可替换 `StartProtection()` 的参数配置。
- 复制 `SignalModule` 模板即可添加新的周期组合。
- 如需细分各模块持仓，可在此基础上叠加子策略或虚拟投资组合管理。
