# Wave Power EA 策略

**Wave Power EA Strategy** 是 MQL4 顾问程序 “Wave Power EA1” 的 C# 版本。原始程序根据随机指标或 MACD 的方向建仓，
并在价格每走固定点数时加仓，同时调整统一的止盈。StockSharp 版本使用高级策略 API、指标绑定和封装好的下单工具重现
这一流程。按照要求，代码中的注释全部保留为英文。

## 工作流程

1. **信号判定**：只有当以下任一指标给出方向时才会打开首笔订单：
   - `Stochastic`：%K 在超卖/超买区域内与 %D 交叉。
   - `MacdSlope`：MACD 主线的斜率由负转正或由正转负。
   - `CciLevels`：CCI 跌破 –120 或升破 +120。
   - `AwesomeBreakout`：Awesome Oscillator 突破记录下来的历史极值。
   - `RsiMa`：快速 SMA 与慢速 SMA 金叉/死叉，同时 RSI 高于或低于 50 作为确认。
   - `SmaTrend`：15/20/25/50 周期 SMA 呈阶梯排列，并满足最小斜率差。

2. **网格扩张**：首笔成交后记录成交价。当价格向持仓不利方向移动 `GridStepPips` 点且尚未达到最大订单数时，策略会按相同
   方向再下市价单，并把成交量乘以 `Multiplier`。

3. **统一目标**：每次加仓都会重新计算共享的止盈与（可选）止损。当持仓数接近 `OrdersToProtect` 时，止盈距离改用
   `ReboundProfitPrimary`；超过该阈值后改用 `ReboundProfitSecondary`，以便更快锁定盈利。

4. **篮子监控**：在每根 K 线收盘时把浮动盈亏换算为“每手多少点”。若达到保护性盈利或亏损阈值，将通过市价单平掉整个
   仓位；当最后一笔订单的存续时间超过 `OrdersTimeAliveSeconds` 或设置为周五禁止开仓时也会执行同样操作。

5. **循环重启**：仓位全部平掉后会清空内部计数器，等待下一次信号重新开始构建网格。

与原版相比，本移植版本不会在第五层之后建立对冲方向的头寸——所有加仓都会沿着初始方向进行。其余资金管理、保护逻辑
和指标过滤与 MQL4 参考实现保持一致。

## 参数

| 参数 | 说明 |
|------|------|
| `EntryLogic` | 首笔订单使用的指标模式。 |
| `CandleType` | 驱动所有指标的时间框架（默认 1 小时）。 |
| `InitialVolume` | 首单的下单量（手数或合约数）。 |
| `GridStepPips` | 网格层之间的最小距离（点）。 |
| `MaxOrders` | 同时允许的最大订单数。 |
| `TakeProfitPips` | 共享止盈距离（点，0 表示禁用）。 |
| `StopLossPips` | 共享止损距离（点，0 表示禁用）。 |
| `Multiplier` | 每次加仓的量增系数。 |
| `SecureProfitProtection` | 是否启用回撤盈利保护。 |
| `OrdersToProtect` | 触发保护前必须累积的订单数量。 |
| `ReboundProfitPrimary` | 第一阶段保护所需的每手盈利（点）。 |
| `ReboundProfitSecondary` | 超过保护阈值后使用的每手盈利（点）。 |
| `LossProtection` | 是否启用浮亏保护。 |
| `LossThreshold` | 在网格满仓时触发保护的每手亏损（点）。 |
| `ReverseCondition` | 反转买卖信号。 |
| `TradeOnFriday` | 是否允许周五开仓。 |
| `OrdersTimeAliveSeconds` | 最新订单允许存在的最长时间（秒，0 表示无限制）。 |
| `TrendSlopeThreshold` | `SmaTrend` 逻辑所需的最小 SMA 斜率差。 |

## 使用建议

1. 在设置了正确价格步长的标的上运行策略，保证点值换算准确。
2. 根据品种波动率和保证金要求调整 `GridStepPips`、`Multiplier` 与 `MaxOrders`。
3. 在真实账户中建议开启保护参数，以避免单边行情造成的巨大回撤。
4. 策略基于收盘价做出决策，请选择符合交易节奏的时间框架（原版常用 M30/H1 组合，默认的 H1 即可）。
5. 由于未实现第五层后的对冲加仓，如需完全复刻原逻辑，可适当降低 `MaxOrders`。

## 文件结构

- `CS/WavePowerEAStrategy.cs`：Wave Power EA 网格策略的 StockSharp 实现。
- `README.md` / `README_ru.md` / `README_cn.md`：英文、俄文、中文说明。

根据任务要求，此策略不提供 Python 版本。
