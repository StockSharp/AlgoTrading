# Double Up Strategy

## Overview
The Double Up strategy is a direct port of the MetaTrader expert advisor `DoubleUp.mq4`. It combines a Commodity Channel Index (CCI) oscillator with the main line of the MACD indicator to detect extreme momentum conditions and then applies a martingale style position sizing model. Whenever both oscillators reach the same extreme zone, the algorithm arms itself for a trade in the opposite direction. Once the CCI returns toward the midpoint, the strategy either opens a new long position (after closing existing shorts) or opens a new short position (after closing existing longs).

The volume of every new position is based on an exponential progression (`baseVolume * 2^lossCounter`). Consecutive losing exits increase the exponent, while a profitable exit resets the progression according to the accumulated wait buffer. This behaviour mirrors the pyramiding logic in the original code where the `pos` and `wait` variables control the applied multiplier.

## Trading Logic
- Subscribe to a single candle series and compute the CCI (default length 8) and MACD main line (default fast 13, slow 33, signal 2).
- Multiply the MACD reading by one million so that its magnitude matches the CCI threshold level.
- When both oscillators exceed `+Threshold`, arm the strategy for a future short entry. When both oscillators drop below `-Threshold`, arm it for a future long entry.
- A pending long entry is executed as soon as the CCI value moves back below `+Threshold`. A pending short entry is executed when the CCI falls below `-Threshold` while the short flag is active, reproducing the exact order of the original code.
- Before opening a new position, the algorithm fully closes any opposite exposure. The new order is dispatched only after all closing trades finish.
- Exit trades are market orders generated during signal reversals. The realised profit or loss of each closed trade feeds the martingale counters.

## Position Sizing Model
- Losing exits increment the loss counter. After the counter reaches `PreWait`, its value is added to the wait buffer and the loss counter is reset to zero.
- A profitable exit transfers the (truncated) wait buffer value into the loss counter and clears the buffer. Future trade sizes therefore start from `2^lossCounter` lots.
- The wait buffer is initialised from `InitialWait` and is otherwise controlled by the above rules.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CciPeriod` | 8 | Period of the Commodity Channel Index. |
| `Threshold` | 230 | Absolute level used to detect oscillator extremes. |
| `MacdFastPeriod` | 13 | Fast EMA length of the MACD calculation. |
| `MacdSlowPeriod` | 33 | Slow EMA length of the MACD calculation. |
| `MacdSignalPeriod` | 2 | Signal EMA length, needed to match the MetaTrader call signature. |
| `BaseVolume` | 0.01 | Starting volume multiplier before applying the martingale exponent. |
| `InitialWait` | 0 | Initial value of the wait buffer (`wait` variable in the original script). |
| `PreWait` | 2 | Minimum number of consecutive losing exits required before the wait buffer accumulates the loss counter. |
| `BackShift` | 0 | Historical shift for indicator readings. Only zero is supported in this port. |
| `CandleType` | 15-minute time frame | Candle type requested from the data feed. Adjust to match the chart timeframe used in MetaTrader. |

## Notes
- The port currently supports only `BackShift = 0`, mirroring the default configuration of the original expert advisor.
- Every order submission and closure uses market orders. Attach separate protections (stop-loss, take-profit) if needed.
- Because the strategy doubles position size after losing trades, ensure that margin limits and risk controls are appropriate for the traded instrument.
