# Arrows & Curves Strategy

## Overview
This strategy is a conversion of the MQL5 expert advisor **Exp_Arrows_Curves**.
It builds a dynamic price channel using recent highs and lows and reacts to
breakouts. The strategy can open or close positions depending on user
permissions and trend direction.

## Strategy Logic
- Calculate highest high and lowest low over the configured period.
- Expand the range by a percentage to form outer channel lines.
- Create inner stop lines using an additional percentage.
- When price breaks above the upper channel, go long; when it breaks below
  the lower channel, go short.
- Inner stop lines trigger position exits when the opposite side of the
  channel is crossed.

## Parameters
- `SspPeriod` – lookback period for highs and lows.
- `Channel` – expansion percentage for main channel lines.
- `StopChannel` – additional percent used for inner stop lines.
- `CandleType` – candle timeframe.
- `BuyPosOpen` / `SellPosOpen` – allow opening long/short positions.
- `BuyPosClose` / `SellPosClose` – allow closing long/short positions.

## Indicators
- Highest
- Lowest

## Notes
The strategy operates on finished candles only. Stop loss and take profit
management are not included; exits rely on channel crossings.
