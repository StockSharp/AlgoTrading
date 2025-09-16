# Exp UltraFATL 双向策略

## 概述
**Exp UltraFATL 双向策略** 是对 MetaTrader 5 专家顾问 `Exp_UltraFatl_Duplex` 的 C# 版本。策略为多头和空头各自运行一套 UltraFATL 指标管线，计算平滑后的 FATL 梯形序列中有多少层向上或向下，从而判断买卖力量的相对强弱并生成交易信号。

## 交易逻辑
1. 为每个方向订阅所配置的蜡烛时间框。
2. 使用 39 阶 FATL 数字滤波器处理所选价格（收盘价、典型价、DeMark 价等）。
3. 将滤波结果送入多个平滑器组成的梯形结构，平滑方法与长度增量可配置。
4. 比较梯形中相邻两个值，统计上升票数和下降票数，并对两个计数器再次平滑。
5. 在设定的信号偏移（默认上一根完整蜡烛）上评估计数器：
   - **多头模块**：上一根蜡烛多头票数大于空头，同时当前蜡烛票数发生下穿（多头 ≤ 空头）时开多；当上一根蜡烛空头票数占优时平多。
   - **空头模块**：上一根蜡烛空头票数占优，同时当前蜡烛票数上穿（多头 ≥ 空头）时开空；当上一根蜡烛多头票数占优时平空。
6. 若设置了止损或止盈，按合约最小变动价位在蜡烛最高/最低价上触发。

策略采用净头寸模式：在开多前会先平掉现有空单，反之亦然，所有进出场都使用市价单。

## 参数
### 多头模块
- **Long Volume** – 开多时的下单数量。
- **Allow Long Entries** – 是否允许新的多头头寸。
- **Allow Long Exits** – 是否允许在反向信号上平多。
- **Long Candle Type** – 多头 UltraFATL 使用的时间框。
- **Long Applied Price** – 送入 FATL 的价格类型。
- **Long Trend Method / Start Length / Phase / Step / Steps** – 梯形平滑配置。
- **Long Counter Method / Counter Length / Counter Phase** – 多头/空头票数的再次平滑设置。
- **Long Signal Bar** – 读取信号时向前偏移的已完成蜡烛数量（小于 1 时按 1 处理）。
- **Long Stop (pts)** – 多头止损点数（按最小价位计算）。
- **Long Target (pts)** – 多头止盈点数。

### 空头模块
参数与多头模块对称，包括 **Short Volume**、**Allow Short Entries/Exits**、**Short Candle Type**、**Short Applied Price**、梯形与计数器平滑设置、**Short Signal Bar**、以及止损/止盈点数。

## 实现说明
- 平滑方法映射到 StockSharp 中的指标实现。Jurik 相关选项使用 `JurikMovingAverage`，`Parabolic` 与 `T3` 由于原始自定义滤波器不可用，使用指数或 Jurik 平滑近似实现。
- 止损/止盈在蜡烛数据上判断，并非向交易服务器发送真实保护单。
- 由于策略只处理已完成蜡烛，信号偏移小于 1 根蜡烛时与设置为 1 的效果相同。
- 策略会为两个计数器曲线创建图表区域，便于可视化观察。

## 使用方法
在 StockSharp 项目中添加该策略，按需求配置多头与空头模块的参数，然后在 Designer、Shell 或 Runner 中运行。确保标的提供相应的蜡烛数据，并为 `LongVolume` 与 `ShortVolume` 设置合适的下单数量。
