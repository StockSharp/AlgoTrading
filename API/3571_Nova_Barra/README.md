# Nova Barra

## Overview
Nova Barra is a direct port of the MetaTrader 5 example `NovaBarra.mq5`. The original script showcases a reusable pattern for detecting the very first tick processed by an expert advisor and distinguishing it from subsequent completed bars. The StockSharp version keeps the logic purely event-driven by subscribing to finished candles via the high-level API and reacting when their open time changes.

The strategy does not submit any orders. Instead, it writes informative log messages whenever a new bar is discovered and when updates arrive inside the still-forming bar. This makes it a lightweight diagnostic helper that can be embedded into larger strategies when quick feedback about candle boundaries is required.

## Strategy logic
### Candle subscription
* **Configurable time frame** – one parameter exposes the candle type (default: 1-minute time frame). The strategy subscribes to this feed as soon as it starts.
* **Finished bar filter** – only candles with the state `Finished` are considered, mirroring the “new bar” condition from MetaTrader.

### Event handlers
* **First detected bar** – the first finished candle initializes the internal reference time and logs that the strategy has picked up an already progressing bar.
* **Normal new bar** – whenever the candle open time changes, the strategy records the new bar and prints its open price.
* **In-bar updates** – if the handler receives a notification for the same candle again (for example, a partial update), it logs that the update belongs to the currently forming bar instead of treating it as a new one.
* **Common follow-up** – both the first and subsequent bars trigger a shared method that prints the open time and close price. This mirrors the placeholder section in the original code where additional actions could be inserted.

## Parameters
* **Candle Type** – selects the candle data series that drives bar detection. Optimisation is disabled because the component is intended as a utility rather than a trading strategy.

## Usage notes
* Attach the strategy to any instrument to observe how StockSharp reports candle completions. The log output will show the first bar, each new bar, and all updates inside the current bar.
* The code is fully commented in English to make the conversion logic transparent and easy to extend.
* No risk management or order placement is performed; the sample focuses exclusively on event detection.
