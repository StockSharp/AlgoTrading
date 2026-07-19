# StockSharp API 策略目录
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

此目录包含使用 C# 和 Python 实现的 StockSharp API 策略示例。策略文件夹按编号范围（`0001-0100`、`0101-0200` 等）划分，下面的页面按主要交易思想对全部策略进行分组。

目录中的每个条目都直接链接到两种实现文件夹，并使用支持明暗主题的透明 SVG 标志。

**策略数量:** 3811

**实现语言:** C# 和 Python

## 策略类型

- [套利、配对与相对价值 (25)](StrategyTypes/arbitrage-pairs-relative-value_zh.md) — 这些策略交易工具、价差或相关资产之间的定价关系，而不是依赖单一方向预测。
- [均值回归与反转 (299)](StrategyTypes/mean-reversion-reversals_zh.md) — 逆势系统寻找价格过度延伸、动能衰竭或趋势失败，并交易价格回归均衡或反转点。
- [突破与通道 (319)](StrategyTypes/breakouts-channels_zh.md) — 策略围绕价格脱离区间、突破支撑阻力或穿越计算通道边界构建。
- [成交量、VWAP 与订单流 (63)](StrategyTypes/volume-vwap-order-flow_zh.md) — 系统利用成交量、VWAP、流动性、市场深度或订单流来确定进出场。
- [K线与价格形态 (191)](StrategyTypes/candlestick-price-patterns_zh.md) — 策略直接从价格行为中识别K线组合、图表结构、跳空、枢轴点及其他重复形态。
- [季节性、交易时段与事件 (92)](StrategyTypes/seasonal-session-event_zh.md) — 时间驱动系统依据交易时段、日历、预定事件、开盘区间或季节性规律运行。
- [统计、自适应与人工智能 (77)](StrategyTypes/statistical-adaptive-ai_zh.md) — 量化策略使用统计估计、自适应模型、机器学习、神经网络或信号分类。
- [因子、投资组合与轮动 (24)](StrategyTypes/factor-portfolio-rotation_zh.md) — 多资产方法对工具排序，按因子配置资金，进行投资组合再平衡或市场轮动。
- [网格、DCA 与仓位管理 (143)](StrategyTypes/grid-dca-position-management_zh.md) — 策略专注于订单阶梯、均价、分阶段入场、仓位规模、退出及持续交易管理。
- [剥头皮与执行 (133)](StrategyTypes/scalping-execution_zh.md) — 短周期系统以进场时机、点差、订单放置和执行质量作为核心优势。
- [波动率与期权 (78)](StrategyTypes/volatility-options_zh.md) — 策略基于波动率状态、区间扩张或收缩、衍生品、期权定价和波动率风险。
- [移动平均线与交叉 (191)](StrategyTypes/moving-averages-crossovers_zh.md) — 趋势系统围绕移动平均线的方向、排列、位移、均线带以及快慢线交叉构建。
- [方向性趋势指标 (264)](StrategyTypes/directional-trend-indicators_zh.md) — 策略由 ADX/DMI、SuperTrend、Parabolic SAR、Ichimoku、Alligator 等方向和趋势强度工具主导。
- [动量与振荡器趋势 (206)](StrategyTypes/momentum-oscillator-trend_zh.md) — 方向性策略由动量、MACD、RSI、CCI、随机指标、ROC、背离及相关振荡器确认。
- [突破、回调与价格行为 (95)](StrategyTypes/breakouts-pullbacks-price-action_zh.md) — 通过突破、回调、通道、波段、K线、回撤和市场结构寻找趋势延续入场。
- [自适应、多时间框架与专用趋势 (277)](StrategyTypes/adaptive-multitimeframe-specialized-trend_zh.md) — 不由单一传统指标主导的自适应、多时间框架、模型驱动、混合和专用趋势系统。
- [振荡器与指标信号 (203)](StrategyTypes/oscillators-indicator-signals_zh.md) — 主要触发来自振荡器、指标阈值、指标交叉或指标背离的策略。
- [订单、风险与仓位管理 (194)](StrategyTypes/order-risk-position-management_zh.md) — 围绕订单处理、规模、保护、网格、恢复、跟踪止损和现有仓位管理的系统。
- [指标组合与信号逻辑 (319)](StrategyTypes/indicator-combinations-signal-logic_zh.md) — 由指标共振、阈值、交叉、背离和信号选择逻辑组成的复合入场规则。
- [价格水平、形态与市场结构 (263)](StrategyTypes/price-levels-patterns-market-structure_zh.md) — 基于价格水平、区间、枢轴点、斐波那契几何、波浪、K线和市场结构的专用系统。
- [量化、自适应与实验性策略 (25)](StrategyTypes/quantitative-adaptive-experimental_zh.md) — 数学、统计、机器学习、自适应、随机化和实验性策略设计。
- [工具、面板、提醒与模板 (74)](StrategyTypes/tools-panels-alerts-templates_zh.md) — 交易工具、界面面板、提醒、模板、测试框架、图表助手、库和集成示例。
- [基本面、宏观与特定资产 (22)](StrategyTypes/fundamental-macro-asset-specific_zh.md) — 与基本面、宏观数据、监管文件、资产类别或特定工具和市场相关的专用逻辑。
- [时间、交易时段与事件规则 (13)](StrategyTypes/time-session-event-rules_zh.md) — 主要约束来自交易时段、时间窗口、日历事件或重复计划的复合策略。
- [方向性与规则驱动交易 (111)](StrategyTypes/directional-rule-based-trading_zh.md) — 主要以明确的多空、买卖、趋势、反转或进出场规则表达的专用方向性系统。
- [复合专家系统 (110)](StrategyTypes/composite-expert-systems_zh.md) — 组合多种机制的多组件、混合、集成、机器人、交易员和专家顾问系统。

## 仓库结构

每个带编号的策略目录都包含策略概述以及 `CS` 和 `PY` 实现文件夹。类型页面提供可搜索的表格、简短说明和标志直达链接。

## 兼容性

这些示例面向 [StockSharp API](https://github.com/StockSharp/StockSharp)，并可适配 StockSharp Designer、Shell 和 Runner 工作流。
