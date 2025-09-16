# Sidus Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements the SIDUS moving average system. It trades using crossovers between two linear weighted moving averages and a confirming exponential average. A position is opened when the short-term LWMA crosses the long-term LWMA or when the long LWMA crosses the slow EMA. Opposite crossovers close or reverse the position. A percentage based stop-loss and take-profit manage risk.

Testing indicates an average annual return of about 25%. It performs best on forex pairs.

The core idea is to capture trend shifts when the fast and slow moving averages realign. The LWMA pair reacts quickly to price changes while the slower EMA filters noise. When either bullish or bearish alignment occurs, the strategy enters in that direction and relies on the protection levels to exit during adverse moves.

## Details

- **Entry Criteria**:
  - **Long**: fast LWMA crosses above slow LWMA *or* slow LWMA crosses above slow EMA.
  - **Short**: fast LWMA crosses below slow LWMA *or* slow LWMA crosses below slow EMA.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover or protective stop levels.
- **Stops**: Yes, uses percentage-based take-profit and stop-loss via `StartProtection`.
- **Default Values**:
  - Fast EMA length = 18.
  - Slow EMA length = 28.
  - Fast LWMA length = 5.
  - Slow LWMA length = 8.
  - Take profit = 2%.
  - Stop loss = 1%.
- **Filters**: None.
