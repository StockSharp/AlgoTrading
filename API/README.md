# StockSharp API Strategy Catalog

This directory contains StockSharp API strategy examples implemented in C# and Python. Strategy folders are split into numbered ranges (`0001-0100`, `0101-0200`, and so on), while the pages below group every strategy by its primary trading idea.

Every catalog entry links directly to both implementation folders and uses a transparent, theme-aware SVG logo.

**Strategies:** 3811

**Implementations:** C# and Python

## Strategy types

- [Arbitrage, Pairs & Relative Value (25)](StrategyTypes/arbitrage-pairs-relative-value.md) — Strategies that trade pricing relationships between instruments, spreads, or linked assets rather than relying on a single directional forecast.
- [Mean Reversion & Reversals (299)](StrategyTypes/mean-reversion-reversals.md) — Counter-trend systems that look for stretched prices, exhausted moves, or failed trends and trade a return toward balance or a reversal point.
- [Breakouts & Channels (319)](StrategyTypes/breakouts-channels.md) — Strategies built around price escaping a range, crossing support or resistance, or moving through a calculated channel boundary.
- [Volume, VWAP & Order Flow (63)](StrategyTypes/volume-vwap-order-flow.md) — Systems that use traded volume, VWAP, liquidity, market depth, or order-flow behaviour to identify entries and exits.
- [Candlestick & Price Patterns (191)](StrategyTypes/candlestick-price-patterns.md) — Strategies that recognize candle formations, chart structures, gaps, pivots, and other recurring patterns directly in price action.
- [Seasonal, Session & Event (92)](StrategyTypes/seasonal-session-event.md) — Time-aware systems driven by sessions, calendars, scheduled events, opening ranges, or recurring seasonal behaviour.
- [Statistical, Adaptive & AI (77)](StrategyTypes/statistical-adaptive-ai.md) — Quantitative strategies using statistical estimation, adaptive models, machine learning, neural networks, or signal classification.
- [Factor, Portfolio & Rotation (24)](StrategyTypes/factor-portfolio-rotation.md) — Multi-asset approaches that rank instruments, allocate capital by factors, rebalance portfolios, or rotate between markets.
- [Grid, DCA & Position Management (143)](StrategyTypes/grid-dca-position-management.md) — Strategies focused on order ladders, averaging, staged entries, position sizing, exits, and ongoing trade management.
- [Scalping & Execution (133)](StrategyTypes/scalping-execution.md) — Short-horizon systems where entry timing, spread, order placement, and execution behaviour are central to the trading edge.
- [Volatility & Options (78)](StrategyTypes/volatility-options.md) — Strategies based on volatility regimes, range expansion or contraction, derivatives, options pricing, and volatility risk.
- [Moving Averages & Crossovers (191)](StrategyTypes/moving-averages-crossovers.md) — Trend systems centered on moving-average direction, alignment, displacement, ribbons, and fast/slow crossover logic.
- [Directional Trend Indicators (264)](StrategyTypes/directional-trend-indicators.md) — Strategies led by ADX/DMI, SuperTrend, Parabolic SAR, Ichimoku, Alligator, and other dedicated trend tools.
- [Momentum & Oscillator Trend (206)](StrategyTypes/momentum-oscillator-trend.md) — Directional strategies confirmed by momentum, MACD, RSI, CCI, stochastic, ROC, and divergence.
- [Breakouts, Pullbacks & Price Action (95)](StrategyTypes/breakouts-pullbacks-price-action.md) — Trend-continuation entries expressed through breakouts, pullbacks, channels, swings, candles, and retracements.
- [Adaptive, Multi-Timeframe & Specialized Trend (277)](StrategyTypes/adaptive-multitimeframe-specialized-trend.md) — Adaptive, model-driven, hybrid, multi-timeframe, and specialized trend systems.
- [Oscillators & Indicator Signals (203)](StrategyTypes/oscillators-indicator-signals.md) — Strategies whose primary trigger comes from oscillators, indicator thresholds, indicator crosses, or indicator divergence.
- [Order, Risk & Position Management (194)](StrategyTypes/order-risk-position-management.md) — Order handling, sizing, protection, grids, recovery, trailing logic, and management of existing positions.
- [Indicator Combinations & Signal Logic (319)](StrategyTypes/indicator-combinations-signal-logic.md) — Composite entries built from indicator agreement, thresholds, crosses, divergences, and signal selection.
- [Price Levels, Patterns & Market Structure (263)](StrategyTypes/price-levels-patterns-market-structure.md) — Specialized systems based on levels, ranges, pivots, Fibonacci geometry, waves, candles, and market structure.
- [Quantitative, Adaptive & Experimental (25)](StrategyTypes/quantitative-adaptive-experimental.md) — Mathematical, statistical, machine-learning, adaptive, randomized, and experimental designs.
- [Tools, Panels, Alerts & Templates (74)](StrategyTypes/tools-panels-alerts-templates.md) — Trading utilities, UI panels, alerts, templates, test harnesses, chart helpers, and integrations.
- [Fundamental, Macro & Asset-Specific (22)](StrategyTypes/fundamental-macro-asset-specific.md) — Logic tied to fundamentals, macro data, filings, asset classes, or named instruments and markets.
- [Time, Session & Event Rules (13)](StrategyTypes/time-session-event-rules.md) — Strategies distinguished by a session, clock window, calendar event, or recurring schedule.
- [Directional & Rule-Based Trading (111)](StrategyTypes/directional-rule-based-trading.md) — Explicit long/short, buy/sell, trend, reversal, and entry/exit rules.
- [Composite Expert Systems (110)](StrategyTypes/composite-expert-systems.md) — Multi-component, hybrid, ensemble, robot, trader, and expert-advisor systems.

## Repository layout

Each numbered strategy directory contains a strategy overview plus `CS` and `PY` implementation folders. The category pages provide the searchable tables with short descriptions and direct logo links.

## Compatibility

The examples are designed for the [StockSharp API](https://github.com/StockSharp/StockSharp) and can be adapted for StockSharp Designer, Shell, and Runner workflows.
