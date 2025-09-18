# Roulette Game
[Русский](README_ru.md) | [中文](README_cn.md)

The Roulette Game strategy recreates the casino-like expert advisor from MetaTrader inside StockSharp. It treats every finished candle as a new spin of the wheel, chooses a random direction, and scales its order size after losses using a Martingale-style progression. The implementation keeps track of a virtual bankroll and limits exposure through configurable caps.

Every round starts by flattening any existing position, flipping a virtual coin to represent red or black, and sending a market order in the selected direction. When the next candle closes, the strategy checks whether the close moved in favor of the bet. Wins reset the stake to the base volume, while losses multiply the stake up to a defined ceiling. A maximum losing streak guard forces a reset before the exposure becomes extreme. Optional cooldown candles can be inserted between rounds to slow the pace of betting.

This conversion focuses on the gambling-inspired money management showcased by the original expert instead of indicator signals. It demonstrates how to orchestrate time-based rounds, maintain internal state, and interact with StockSharp's high-level API through candle subscriptions.

## Details

- **Entry Criteria**: No technical filter. Direction is selected randomly at the end of a finished candle.
- **Long/Short**: Both directions, picked randomly each round.
- **Exit Criteria**: Position closes on the next finished candle, evaluating whether the price closed above or below the entry.
- **Stops**: No traditional stops. Risk is managed with stake caps and streak limits.
- **Default Values**:
  - `BaseVolume` = 1m
  - `LossMultiplier` = 2m
  - `MaxMultiplier` = 16m
  - `RoundCooldown` = 1
  - `MaxLosingStreak` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Money Management
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High

## Notes

- Market orders are sized according to the multiplier-adjusted stake and rounded to the instrument's volume step.
- Wins reset the stake to the base volume; losses scale it by the multiplier until the maximum multiplier or losing streak limit is reached.
- Cooldown bars prevent immediate re-entry and make it possible to synchronize with slower data feeds.
