# Triangular Arbitrage Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy recreates the MetaTrader expert advisor "Triangular Arbitrage" inside StockSharp. It continuously monitors three FX pairs (two legs and the direct cross) and opens a basket of three market orders whenever the implied cross rate diverges from the quoted cross beyond a configurable threshold. The basket is closed as soon as the combined floating profit reaches the desired amount in account currency.

## Core Idea

- Observe the ask prices of the first leg (e.g., EURUSD), the second leg (e.g., USDJPY) and the direct cross (e.g., EURJPY).
- Compute the implied cross price as `FirstLegAsk × SecondLegAsk` and compare it with the direct cross ask.
- When the relative difference exceeds `Threshold`, open a three-leg arbitrage basket:
  - If the implied price is higher (positive difference) go long the cross and short both legs.
  - If the implied price is lower (negative difference) go short the cross and buy both legs.
- Close every position once the floating PnL of the whole basket reaches `ProfitTarget`.

## Data Flow

- **Market data:** Level 1 quotes are required for all three securities. The strategy reads the best bid/ask from the subscribed streams.
- **No candles or indicators:** The logic uses only raw quotes; there are no historical candles or custom buffers involved.

## Trading Rules

### Opportunity Detection

1. Wait until best ask prices are available for all three instruments.
2. Ensure the selected portfolio has at least `MinimumBalance` currency units of equity (`Portfolio.CurrentValue` or `CurrentBalance`).
3. Calculate the relative discrepancy `(ImpliedPrice - CrossAsk) / CrossAsk` and compare it with `Threshold`.

### Order Placement

- Order sizes are fixed by `LotSize`. Before submitting, each volume is aligned to the instrument-specific `VolumeStep`, `MinVolume` and `MaxVolume`. Orders are skipped if the broker constraints make the requested lot impossible.
- For a positive discrepancy (buy cross / sell legs):
  - `BuyMarket` on the cross pair.
  - `SellMarket` on each leg pair.
- For a negative discrepancy (sell cross / buy legs):
  - `SellMarket` on the cross pair.
  - `BuyMarket` on each leg pair.

### Exit Management

- While a basket is open, the strategy tracks:
  - Realized PnL since the basket was initiated (`PnLManager.RealizedPnL`).
  - Floating PnL per instrument, estimated from the average fill price and the latest bid/ask.
- When the sum of realized and floating PnL reaches `ProfitTarget`, the strategy flattens all three instruments with market orders in the opposite direction.
- After every fill the internal average price and volume are updated; once every position is flat the cycle resets and a new opportunity can be taken.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `FirstPair` | *(none)* | Security representing the first FX leg (e.g., EURUSD). |
| `SecondPair` | *(none)* | Security for the second FX leg (e.g., USDJPY). |
| `CrossPair` | *(none)* | Security for the direct cross (e.g., EURJPY). |
| `LotSize` | `0.01` | Requested volume for each order before broker alignment. |
| `ProfitTarget` | `10` | Profit in account currency that closes the arbitrage basket. |
| `Threshold` | `0.0001` | Minimum relative discrepancy between implied and quoted cross rates. |
| `MinimumBalance` | `1000` | Minimum portfolio equity required to allow a new basket. |

## Implementation Notes

- Level 1 subscriptions (`SubscribeLevel1`) drive the logic; the strategy does not store custom collections and does not access indicator buffers, complying with repository rules.
- Position tracking relies on `OnNewMyTrade` events to rebuild signed volumes and average prices for each security without querying historic trades.
- Profit estimation uses `PriceStep`/`StepPrice` when available; otherwise it falls back to a simple price-difference × volume approximation, matching the behavior of the MQL version when broker metadata is missing.
- The basket is protected against repeated submissions by internal flags (`_hasOpenCycle`, `_closeRequested`) so each arbitrage cycle is executed once per detected opportunity.

## Tags & Characteristics

- **Market:** Forex, multi-currency.
- **Style:** Market-neutral arbitrage.
- **Direction:** Both long and short baskets depending on the discrepancy sign.
- **Timeframe:** Tick/quote driven, no candle dependency.
- **Risk Controls:** Equity floor via `MinimumBalance`, immediate closure at `ProfitTarget`, automatic volume normalization.
