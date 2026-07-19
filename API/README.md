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
- [Trend Following & Momentum (1033)](StrategyTypes/trend-following-momentum.md) — Directional systems that follow persistent movement, accelerating price, moving-average structure, or momentum continuation.
- [Oscillators & Indicator Signals (203)](StrategyTypes/oscillators-indicator-signals.md) — Strategies whose primary trigger comes from oscillators, indicator thresholds, indicator crosses, or indicator divergence.
- [Multi-Signal & Other (1131)](StrategyTypes/multi-signal-other.md) — Combined, specialized, educational, or infrastructure-oriented strategies that do not fit cleanly into one primary trading family.

## Repository layout

Each numbered strategy directory contains a strategy overview plus `CS` and `PY` implementation folders. The category pages provide the searchable tables with short descriptions and direct logo links.

## Compatibility

The examples are designed for the [StockSharp API](https://github.com/StockSharp/StockSharp) and can be adapted for StockSharp Designer, Shell, and Runner workflows.
