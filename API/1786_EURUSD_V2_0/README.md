# EURUSD V2.0 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Mean-reversion system for EURUSD using a long-term simple moving average (SMA) and volatility filter based on Average True Range (ATR).

## Strategy Logic

- Calculate an SMA of length *MA Length* on the chosen candle type.
- Enter **short** when price is above the SMA and pulls back within *Buffer* pips while ATR is below *ATR Threshold*.
- Enter **long** when price is below the SMA and approaches it within *Buffer* pips with low ATR.
- Position size is derived from account balance and *Risk Factor Z*.
- Stop-loss and take-profit are placed in fixed pip distances from entry.
- After exit, the system waits for price to move *Noise Filter* pips away from the entry level before a new trade is allowed.

## Parameters

- **MA Length** – period for the simple moving average (default 218).
- **Buffer (pips)** – maximum distance from SMA to trigger entry (default 0).
- **Stop Loss (pips)** – stop loss distance from entry (default 20).
- **Take Profit (pips)** – take profit distance from entry (default 350).
- **Noise Filter (pips)** – distance to reset trading permission (default 50).
- **ATR Length** – ATR calculation period (default 200).
- **ATR Threshold (pips)** – maximum ATR to allow new positions (default 40).
- **Max Spread (pips)** – maximum allowed spread (default 4).
- **Risk Factor Z** – money management factor (default 2).
- **Candle Type** – timeframe of processed candles (default 15 minutes).

This strategy uses market orders for entries and exits.

