# Volume Weighted MA Digit System Strategy

This strategy replicates the **Volume Weighted MA Digit System**. It builds two volume weighted moving averages (VWMA) based on candle highs and lows. The price crossing these bands provides trading signals.

## How It Works

1. **Indicators**
   - `VWMA High`: VWMA applied to candle highs.
   - `VWMA Low`: VWMA applied to candle lows.
2. **Signals**
   - **Long Entry**: Close price crosses above `VWMA High`.
   - **Short Entry**: Close price crosses below `VWMA Low`.
   - Opposite cross closes open positions.
3. **Risk Management**
   - Uses built‑in `StartProtection` with configurable stop loss and take profit (points).

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `VwmaPeriod` | VWMA calculation length | `12` |
| `CandleType` | Candle timeframe used for calculation | `4h` |
| `StopLoss` | Stop loss in points | `1000` |
| `TakeProfit` | Take profit in points | `2000` |

## Notes

- Only closed candles are processed.
- Strategy uses high level API features such as `SubscribeCandles`, `Bind` and standard indicators.
- Original MQL strategy: `Exp_Volume_Weighted_MA_Digit_System.mq5`.
