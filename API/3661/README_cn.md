# 经典与虚拟跟踪止损策略

## 概述
**经典与虚拟跟踪止损策略** 是将 MetaTrader 专家顾问 `Classic & Virtual Trailing.mq4`（MQL ID 49326）转换成的 C# 版本。
与原始 EA 一样，本策略不会开仓，只负责在已有仓位上应用两种可选的跟踪止损管理模式：

- **Classic（经典）** – 模拟 MetaTrader 自带的跟踪止损机制，让真实止损价格始终跟随市场。当价格回踩到
  跟踪距离时，策略会以市价平掉仓位，效果与服务器端止损相同。
- **Virtual（虚拟）** – 复刻源 EA 中的虚拟分支，完全在本地记录跟踪止损，不修改交易所端的止损单。当
  价格触及虚拟水平时，策略会手动以市价平仓。

两种模式的激活条件完全一致：只有当价格先向有利方向移动了 `TrailingStartPips` + `TrailingGapPips` 个点时，
才会生成跟踪止损，并保持在当前价格之后 `TrailingGapPips` 点的位置。本移植版本通过 `PriceStep`（对于 3/5
位外汇报价会额外乘以 10）将 MetaTrader 风格的“点”转换为实际价格偏移。

## 交易逻辑
1. **订阅 Level1** – 策略只监听最新买卖价，每个行情跳动都会触发更新，不需要蜡烛或盘口深度数据。
2. **点值换算** – 所有参数均以“点”为单位，通过 `GetPipSize()` 方法换算为价格偏移，算法与 MetaTrader 保持一致。
3. **跟踪激活** – 当浮动盈利至少达到 `TrailingStartPips + TrailingGapPips` 后，在多头创建 `Bid − TrailingGap`，在空头创建
   `Ask + TrailingGap` 的跟踪水平。
4. **最小止损距离（经典模式）** – 如果交易商通过 Level1 字段（如 `StopLevel` 或 `StopDistance`）提供最小止损距离，策略会自动
   放大间距，确保符合限制后再更新跟踪止损。
5. **跟踪维护** – 只有在价格继续向有利方向运动时才会抬高/下移跟踪水平，绝不会倒退。多头和空头的跟踪值分开存储。
6. **退出执行** – 当价格触及跟踪水平时，策略会立即以市价平仓。由于 StockSharp 在客户端执行止损，此行为对两种模式都适用。
7. **仓位重置** – 净仓位归零时会清除缓存的跟踪值，为下一笔交易做好准备，对应源 EA 中“遍历订单并重置”的逻辑。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TrailingMode` | 选择 `Classic`（真实止损）或 `Virtual`（虚拟平仓）。 | `Virtual` |
| `TrailingStartPips` | 启动跟踪止损所需的盈利点数。 | `30` |
| `TrailingGapPips` | 市价与跟踪止损之间保持的距离（点）。 | `30` |

所有参数都通过 `StrategyParam<T>` 声明，可在 StockSharp Designer 中参与优化。

## 实现说明
- 策略基于净仓位（`Strategy.Position`）工作，这是 StockSharp 的标准模式，也是对 MetaTrader“遍历所有订单”的等价实现。
- 跟踪水平完全依赖最新买卖价，因此请确保标的能够提供 Level1 数据。
- 经典模式在可能的情况下会遵循经纪商的最小止损距离；若行情源未提供该信息，则按用户输入的间距执行。
- 由于 StockSharp 的图表系统与 MetaTrader 不同，移植时没有绘制任何水平线或文本对象。
- 代码遵守项目指南，不访问历史指标缓冲区，避免额外的集合计算。

## 文件
- `CS/ClassicVirtualTrailingStrategy.cs` – 策略实现。
- `README.md`, `README_cn.md`, `README_ru.md` – 多语言文档。
