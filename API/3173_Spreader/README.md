# Spreader Strategy

## Overview

The **Spreader Strategy** is a pair-trading approach inspired by the original MetaTrader expert advisor "Spreader". The strategy
monitors two positively correlated instruments and seeks to profit from short-term divergences while maintaining a market-neutral
profile. Once the combined position reaches the desired money target the strategy closes both legs and waits for the next setup.

The algorithm is designed for one-minute candles by default, mirroring the behaviour of the original EA, but the timeframe can be
adjusted when the strategy is loaded into Designer, Shell, or the API runner.

## Trading Logic

1. **Data preparation**
   - Subscribes to candles for the primary security (the one assigned to the strategy) and the secondary security.
   - Stores the latest `2 * ShiftLength + 1` close values for each instrument. The default shift length is 30 bars.
   - Only reacts to completed candles and requires both instruments to produce a bar with the same opening time.

2. **Trend filter**
   - Computes the price changes between the current close and the close `ShiftLength` bars ago, as well as the change between
     the middle and oldest samples for both instruments.
   - If the two changes for either instrument have the same sign the strategy interprets it as a persistent trend and skips the
     trade.

3. **Correlation check**
   - Ensures the sign of the latest change on both instruments is identical. If the sign differs the correlation is considered
     negative and no spread is opened.

4. **Volatility alignment**
   - Calculates the absolute magnitude of the recent swings (`a` for the primary leg, `b` for the secondary leg) and uses their
     ratio to scale the hedge volume. Ratios outside the `[0.3, 3]` range are rejected because they indicate unstable behaviour.

5. **Entry**
   - Chooses the direction of the primary leg by comparing the normalized swings: if the primary move is stronger the strategy
     buys the primary instrument and sells the secondary leg, otherwise it sells the primary leg and buys the secondary.
   - Orders are sent with market execution and volumes are normalised to respect lot step, minimum, and maximum limits of each
     security.

6. **Position management**
   - If only the secondary leg is open (for example due to connectivity issues) the strategy opens the missing primary leg in the
     opposite direction to restore the hedge.
   - If only the primary leg remains it is closed immediately to avoid directional exposure.
   - When both legs are present the strategy monitors the combined floating profit and closes both positions once the configured
     money target is reached.

7. **Safety checks**
   - Trading is disabled when the contract size (multiplier) of the two securities differs, since the original EA assumes equal
     contract specifications.
   - All trading requests are ignored until the strategy is connected, synchronized, and allowed to trade by the hosting
     environment (`IsFormedAndOnlineAndAllowTrading`).

## Parameters

| Parameter | Description |
|-----------|-------------|
| `SecondSecurity` | Instrument that forms the hedge leg of the spread. This parameter is required. |
| `PrimaryVolume` | Base order volume for the primary instrument. The secondary volume is scaled using the swing ratio. |
| `TargetProfit` | Absolute profit, expressed in the account currency, after which both legs are closed. |
| `ShiftLength` | Number of bars used when comparing recent swings. The strategy uses `2 * ShiftLength + 1` candles from each instrument. |
| `CandleType` | Candle series used for analysis. Defaults to 1-minute time frame. |

## Tips

- The strategy works best on instruments with stable positive correlation and similar volatility profiles (for example, highly
  related currency pairs or index futures).
- Contract specifications should be aligned (tick size, lot step, margin), otherwise volume normalisation may reduce order sizes
  significantly.
- Because the strategy relies on candle data, ensure both instruments receive synchronized bar updates from the data provider.

## Requirements

- Two liquid instruments with positive correlation.
- Access to market data and trading permissions for both instruments through StockSharp connectors.
- Portfolio assigned to the strategy instance.
