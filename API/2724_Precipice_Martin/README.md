# Precipice Martin Strategy (C#)

## Overview

The Precipice Martin strategy is a mechanical grid approach that opens one market order at the close of every processed candle. The original MetaTrader 5 expert advisor created a symmetrical buy and sell position on every new bar and managed exits using static stop-loss and take-profit offsets that were expressed in pips. Losing trades increased the next order size by a martingale multiplier, while profitable trades reset the position size to the minimum lot.

This C# port follows the same high-level logic using the StockSharp high-level API. For each finished candle the strategy:

1. Updates existing long and short positions and closes them if the candle range pierced the configured stop-loss or take-profit level.
2. When flat, alternates between opening a long or a short market position (when both directions are enabled) to emulate the dual-entry behaviour of the source robot while remaining compatible with StockSharp's net-position accounting.
3. Applies optional martingale sizing so that consecutive losing trades increase volume by the configured multiplier.
4. Computes stop-loss and take-profit targets from user-defined pip distances that are translated to absolute price offsets based on the security tick size.

## Conversion Notes

* The original EA opened a long and a short position on every new bar when both toggles were enabled. Because StockSharp uses net positions by default, the C# version alternates between directions on consecutive opportunities to avoid instantly flattening the net position. This still ensures both sides of the market are traded over time.
* Stop-loss and take-profit management is performed internally by checking whether a candle's high/low would have triggered the corresponding level. When a level is hit the strategy closes the position using a market order and records the realised profit or loss for the martingale logic.
* Lot validation replicates the `LotCheck` routine from MQL5 by rounding the calculated volume to the exchange `VolumeStep`, enforcing the minimum and maximum bounds, and cancelling the order if the rounded value becomes zero.
* The martingale routine mirrors `CalculateLot`: any non-profitable exit multiplies the next order size by `MartingaleCoefficient`, while a profitable exit resets the multiplier to one.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Use Buy** | Enables opening long positions. |
| **Buy SL/TP (pips)** | Distance (in pips) used for both the stop-loss and take-profit of long trades. A value of 0 disables exits for that side. |
| **Use Sell** | Enables opening short positions. |
| **Sell SL/TP (pips)** | Distance (in pips) used for both the stop-loss and take-profit of short trades. |
| **Use Martingale** | Toggles martingale position sizing. When disabled every order uses the minimum lot size. |
| **Martingale Coefficient** | Multiplier applied to the minimum lot after each non-profitable trade. |
| **Candle Type** | Timeframe of the candles processed by the strategy. By default the strategy works on one-minute bars but any available timeframe can be selected. |

## Trading Logic

1. **Pip Size Calculation** – the strategy derives the pip value from the security tick size. For instruments quoted with fractional pips (5-digit FX symbols) the pip is considered 10 ticks, matching the MT5 implementation.
2. **Entry Selection** – if both `Use Buy` and `Use Sell` are enabled, the strategy alternates between long and short entries whenever it is flat. If only one direction is enabled, all trades follow that direction. Entries are triggered immediately after a candle is completed and the strategy is online.
3. **Stop/Take Levels** – when a trade is opened, the stop-loss and take-profit are stored as absolute prices relative to the entry using the selected pip distance. A value of zero disables both levels for that direction.
4. **Exit Handling** – on each finished candle the high/low values are checked. If the low breaches the long stop or the high breaches the long target, the long position is closed. For shorts the logic is mirrored. Exits are executed with market orders using the last recorded volume for that position.
5. **Martingale Sizing** – the next order volume equals the instrument minimum lot multiplied by the current martingale multiplier. Losing trades (including break-even results) multiply the multiplier by `MartingaleCoefficient`; profitable trades reset it to one. Volume rounding to the exchange step is applied before the order is submitted.
6. **Safety Checks** – if the rounded volume is below the exchange minimum lot the order is skipped, preventing "not enough money" errors that the original EA handled via `CheckVolume`.

## Usage Guidelines

1. Configure the desired timeframe in **Candle Type** to match the chart period used in MT5.
2. Adjust the pip distances to match the desired stop-loss and take-profit behaviour. Remember that the offsets are absolute prices, so the actual stop in currency depends on the symbol.
3. Enable or disable martingale sizing according to your risk tolerance. Because volume grows exponentially after consecutive losses, apply conservative multipliers.
4. Deploy the strategy on a security that provides real-time candles. The strategy requires completed bars to operate and will not trade on incomplete candles.
5. Monitor margin usage when martingale is active. The StockSharp version intentionally alternates directions when both sides are enabled, so only one net position is open at any moment.

## Differences from the MT5 Implementation

* **Net Positions** – the alternation logic replaces the simultaneous hedged entries of the original algorithm. If a true hedging account is required you can run two instances of the strategy (one with `Use Buy`, another with `Use Sell`).
* **Order Placement** – protective orders are not placed on the exchange book. Instead, exits are executed via market orders when the strategy detects that the stop or take level was crossed.
* **History Scan** – the MT5 script recalculated the martingale coefficient by scanning the entire trade history on every tick. The C# version maintains the multiplier incrementally to reduce overhead while preserving behaviour.

## Risk Disclaimer

Martingale-based strategies can generate very large positions during losing streaks, which can exceed account risk limits. Always test the strategy on simulated data before live deployment and ensure that the selected multiplier and pip distances suit the volatility of the traded instrument.
