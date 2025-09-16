# Forex Currency Power Strategy

## Overview
The **Forex Currency Power Strategy** replicates the MetaTrader *FOREX Currency Power* dashboard inside StockSharp. It measures the relative strength of five major currencies (EUR, USD, GBP, CHF, and JPY) by combining the normalized momentum of four major pairs per currency. The strategy then compares the power values of the traded pair's base and quote currencies to capture the strongest-versus-weakest relationship.

Unlike the original script, which only drew a panel, this port provides an executable trading logic. When the base currency outperforms the quote currency by a configurable margin the strategy opens a long position, and it flips short when the quote currency becomes stronger. Positions are closed when the strength spread contracts below the configured exit band. All orders are issued as market orders so that the relative-strength signals are executed immediately.

## Core ideas inherited from MQL
- Use one-minute candles and evaluate only completed bars.
- Compute each currency's strength as the average of four weighted pairs:
  - **EUR**: EURUSD, EURGBP, EURCHF, EURJPY
  - **USD**: EURUSD, GBPUSD, USDCHF, USDJPY (EURUSD and GBPUSD inverted)
  - **GBP**: EURGBP, GBPUSD, GBPCHF, GBPJPY (EURGBP inverted)
  - **CHF**: EURCHF, USDCHF, GBPCHF, CHFJPY (EURCHF, USDCHF, GBPCHF inverted)
  - **JPY**: EURJPY, USDJPY, GBPJPY, CHFJPY (all inverted)
- Normalize each pair by positioning the closing price within the high/low range of the latest *N* candles, returning a 0–100 reading just like the MetaTrader custom symbol.

## Implementation details
- **Indicator stack** – Each FX pair receives a dedicated `Highest` and `Lowest` indicator with a shared lookback. Binding is performed through `SubscribeCandles(...).BindEx(...)` in accordance with the high-level API requirements from the instructions.
- **Currency aggregation** – Currency power values are recomputed every time one of the pair subscriptions reports a new normalized value. Contributions with negative weights are inverted (`100 - value`) so that all contributions keep a 0–100 scale.
- **Trading logic** – The strategy is driven by candles from `Strategy.Security`. It waits until all currency powers are available, verifies that trading is allowed, and then compares the base and quote strength. Long and short entries cancel any working orders, close an opposite position if required, and place market orders.
- **Logging** – A compact log line summarises the current power table once per finished main candle to help monitor the conversion fidelity without drawing a custom chart.

## Parameters
| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `CandleType` | Data type used for all candle subscriptions. | 1-minute time frame | No |
| `Lookback` | Number of candles in the rolling high/low range. | 5 | Yes (3 → 20 step 1) |
| `EntryThreshold` | Minimum difference (base – quote) that triggers a directional entry. | 15 | Yes (5 → 30 step 5) |
| `ExitThreshold` | Absolute difference below which any open trade is closed. | 5 | Yes (2 → 15 step 1) |
| `BaseCurrency` | ISO code of the currency on the long side of the traded pair. | `EUR` | No |
| `QuoteCurrency` | ISO code of the currency on the short side of the traded pair. | `USD` | No |
| `EURUSD`, `EURGBP`, `EURCHF`, `EURJPY`, `GBPUSD`, `USDCHF`, `USDJPY`, `GBPCHF`, `GBPJPY`, `CHFJPY` | Securities required to build the currency baskets. | *(required)* | No |

## Usage notes
1. Assign all ten FX securities plus the traded instrument before starting the strategy. The base and quote parameters should match the ISO codes of `Strategy.Security` (for example `EUR`/`USD` when trading EURUSD).
2. The strategy requires at least `Lookback` candles for each pair before it starts trading. During this warm-up period the power table stays incomplete and the entry logic remains idle.
3. Entry and exit thresholds are expressed in the original 0–100 power scale. Setting `ExitThreshold` lower than `EntryThreshold` prevents rapid flip-flopping when the spread oscillates around the entry level.
4. Because all trades are market orders, risk controls such as `StartProtection()` are enabled on start, and users can further manage position size through standard `Strategy.Volume` settings.

## Conversion notes
- High-level StockSharp API (`SubscribeCandles().BindEx`) replaces the original timer loop and manual price arrays.
- Indicator values are consumed only when `IsFinal` is true, ensuring that unfinished candles never distort the 0–100 scale.
- The MetaTrader graphical panel is replaced with structured logging and automated trades to fit the StockSharp strategy template.

