# Exp Skyscraper Fix + ColorAML + X2MA Candle MMRec 策略

## 概览
- 将 MetaTrader 专家顾问 **Exp_Skyscraper_Fix_ColorAML_X2MACandle_MMRec** 移植到 C# / StockSharp。
- 同时使用三种颜色型过滤器：Skyscraper Fix 通道、ColorAML 自适应市场水平以及双层平滑的 X2MA 蜡烛。
- 三个模块共享同一交易品种，可独立发出开仓或平仓信号，从而实现趋势跟随与快速反转的组合交易。
- 资金管理模块会在同一方向连续亏损时自动把下单手数切换到较小的 `SmallMM` 数值。

## 策略逻辑
### Skyscraper Fix 模块
1. 根据 ATR 波动和所选价格类型（最高/最低或收盘价）构建 Skyscraper Fix 追踪通道。
2. 通道颜色转为多头时：
   - 若允许，会平掉当前持有的空单；
   - 在等待设定的信号延迟后，可开立新的多单。
3. 颜色变为空头时，逻辑对称应用于空头方向。
4. 通道的上下包络、ATR 放大系数 `Kv` 与百分比偏移完全复刻原始指标。

### ColorAML 模块
1. 通过计算两个连续分形窗口的波动区间，并对组合价格进行自适应平滑，得到 AML 数值。
2. 指标输出三种颜色：`2` 表示多头，`0` 表示空头，`1` 表示中性；中性蜡烛不会触发操作。
3. 当颜色变为多头时，可在上一根被检查的蜡烛不是多头颜色的前提下平空并开多（视参数而定）。
4. 颜色变为空头时，执行空头侧的对称操作。

### X2MA 蜡烛模块
1. 对 OHLC 四个价格分别进行两次可配置的移动平均平滑，生成合成蜡烛。
2. 颜色由平滑后的蜡烛实体决定：收盘价高于开盘价为多头，低于为空头，相等则为中性。
3. 以价格步长为单位的微小实体会被“Gap” 阈值抹平，避免频繁换色。
4. 多头颜色会平空并允许开多，空头颜色会平多并允许开空。

### 资金管理
1. 每个模块分别记录自身多头和空头交易的结果。
2. 关闭仓位时会统计该笔交易是否亏损。
3. 如果最近 `Loss Trigger` 次同方向交易全部亏损，则下一笔该方向的下单手数切换为 `SmallMM`。
4. 一旦出现盈利或持平的交易，亏损序列被打破，手数自动恢复为默认的 `MM`。

## 参数
| 模块 | 参数 | 说明 | 默认值 |
| --- | --- | --- | --- |
| Skyscraper | `Skyscraper Candle` | Skyscraper Fix 指标使用的 K 线周期。 | 4 小时 |
| Skyscraper | `Skyscraper Length` | ATR 平均窗口长度。 | 10 |
| Skyscraper | `Skyscraper Kv` | ATR 步长的灵敏度乘数。 | 0.9 |
| Skyscraper | `Skyscraper Percentage` | 在中线基础上增加/减少的百分比偏移。 | 0 |
| Skyscraper | `Skyscraper Mode` | 构建包络所使用的价格（高/低或收盘）。 | 高/低 |
| Skyscraper | `Skyscraper Signal Bar` | 在响应颜色前需要等待的已收盘蜡烛数量。 | 1 |
| Skyscraper | `Skyscraper Buy` / `Skyscraper Sell` | 是否允许 Skyscraper 模块开多 / 开空。 | true |
| Skyscraper | `Skyscraper Close Long` / `Skyscraper Close Short` | 是否允许该模块平多 / 平空。 | true |
| Skyscraper | `Skyscraper Normal Volume` | 默认下单手数（对应 EA 中的 `MM`）。 | 0.1 |
| Skyscraper | `Skyscraper Reduced Volume` | 连续亏损后使用的降级手数（`SmallMM`）。 | 0.01 |
| Skyscraper | `Skyscraper Buy Loss Trigger` / `Skyscraper Sell Loss Trigger` | 触发降级手数所需的连续亏损次数。 | 2 |
| ColorAML | `ColorAML Candle` | ColorAML 指标使用的 K 线周期。 | 4 小时 |
| ColorAML | `ColorAML Fractal` | 计算波动区间的分形窗口长度。 | 6 |
| ColorAML | `ColorAML Lag` | 控制自适应平滑强度的滞后参数。 | 7 |
| ColorAML | `ColorAML Signal Bar` | 读取消息时向后偏移的蜡烛数量。 | 1 |
| ColorAML | `ColorAML Buy` / `ColorAML Sell` | 是否允许 ColorAML 模块开多 / 开空。 | true |
| ColorAML | `ColorAML Close Long` / `ColorAML Close Short` | 是否允许该模块平多 / 平空。 | true |
| ColorAML | `ColorAML Normal Volume` / `ColorAML Reduced Volume` | 模块的默认与降级手数。 | 0.1 / 0.01 |
| ColorAML | `ColorAML Buy Loss Trigger` / `ColorAML Sell Loss Trigger` | 连续亏损次数阈值。 | 2 |
| X2MA | `X2MA Candle` | X2MA 合成蜡烛使用的时间框架。 | 4 小时 |
| X2MA | `First Method` / `Second Method` | 第一层与第二层平滑的移动平均类型。 | SMA / JJMA |
| X2MA | `First Length` / `Second Length` | 两层平滑的周期长度。 | 12 / 5 |
| X2MA | `First Phase` / `Second Phase` | Jurik 平滑使用的兼容相位参数。 | 15 |
| X2MA | `Gap Points` | 以价格步长计的实体抹平阈值。 | 10 |
| X2MA | `X2MA Signal Bar` | 在读取颜色前向后查看的蜡烛数量。 | 1 |
| X2MA | `X2MA Buy` / `X2MA Sell` | 是否允许 X2MA 模块开多 / 开空。 | true |
| X2MA | `X2MA Close Long` / `X2MA Close Short` | 是否允许该模块平多 / 平空。 | true |
| X2MA | `X2MA Normal Volume` / `X2MA Reduced Volume` | 模块的默认与降级手数。 | 0.1 / 0.01 |
| X2MA | `X2MA Buy Loss Trigger` / `X2MA Sell Loss Trigger` | 连续亏损次数阈值。 | 2 |

## 使用建议
1. 请根据品种波动调整各模块的时间框架（例如日内可用 1 小时，波段可用 4 小时）。
2. 三个模块可分别启用或禁用，关闭其中一个不影响其余模块继续工作。
3. 连亏阈值偏保守，如果交易品种趋势性较强，可适当调高阈值以保持默认手数。
4. 策略仅在蜡烛收盘后行动，请确保输入的数据与配置的时间框架一致。
