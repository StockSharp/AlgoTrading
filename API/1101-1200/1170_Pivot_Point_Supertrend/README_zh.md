# Pivot Point Supertrend 枢轴超级趋势
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略结合枢轴点与基于ATR的Supertrend来捕捉趋势反转。

枢轴点形成动态中心线，ATR因子生成上下轨并跟随价格。当趋势反转时策略顺势入场。

## 详情
- **入场条件**: 基于枢轴点和ATR Supertrend 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Pivot Points, ATR
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

## 计算与执行

仅使用已完成的蜡烛。枢轴高点/低点必须严格高于/低于左右各 `PivotPeriod` 根蜡烛，且右侧蜡烛全部完成后才确认。如果同一根蜡烛同时符合两个条件，高点优先。首个枢轴初始化 `center`，后续按 `(2 * center + pivot) / 3` 更新。

枢轴带使用 Wilder ATR：前 `AtrPeriod` 个真实波幅取平均，此后以 `1 / AtrPeriod` 权重平滑。基础支撑/阻力为 `center ± AtrFactor * ATR`。前收盘价高于前支撑时，支撑取基础值与前值的最大值，否则重置；前收盘价低于前阻力时，阻力取最小值，否则重置。收盘价严格高于前阻力则转为上行，严格低于前支撑则转为下行，否则保留方向。初始方向为上行，初始化不产生信号。

Wilder ATR 和确认的枢轴中心必须就绪。仅枢轴带方向切换触发入场：上行做多，下行做空。初始方向为上行，初始化不下单。每个反向信号通过一笔数量为 `Volume + abs(Position)` 的市价订单完全反转仓位；空仓入场使用 `Volume`。没有 SMA、独立价格 SuperTrend、日期过滤、冷却时间或保护止损。

枢轴概念来源：[LonesomeTheBlue 的 Pivot Point Supertrend](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/)。上述规则定义本示例的双向模型。
