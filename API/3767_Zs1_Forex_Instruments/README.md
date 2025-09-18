# Zs1 Forex Instruments Strategy

This strategy reproduces the hedged grid logic of the MetaTrader expert **Zs1_www_forex-instruments_info**. The algorithm opens a simultaneous buy/sell pair, monitors how far price travels from the starting point and reacts to five discrete trading zones. The surviving leg of the hedge is averaged with martingale multipliers while the basket is protected by an equity-based exit.

## Core behaviour

- Open an initial market hedge (one buy and one sell) with the configured base volume.
- Once either leg becomes profitable, close it and keep the losing side as the anchor order.
- Track price displacement using the `Orders Space (pips)` parameter. When a new zone is reached, execute the same branching logic as the original expert:
  - Zone −2: close the basket on profit, otherwise average against the move.
  - Zone −1: add a position opposite to the initial anchor.
  - Zone 0: add a position in the direction of the anchor.
  - Zone +1: close the basket on profit, otherwise open the opposite side.
- Whenever three or more trades are active, immediately exit if the floating profit is non-negative.
- After all positions are closed the cycle restarts automatically.

## Parameters

| Name | Description |
| --- | --- |
| `Orders Space (pips)` | Distance in pips between adjacent grid levels. |
| `Zone Offset (pips)` | Extra buffer that must be breached before a new zone is confirmed. |
| `Initial Volume` | Base volume used for the opening hedge and for martingale scaling. |

## Notes

- The martingale multipliers follow the original tunnel sequence (1, 3, 6, 12, ...).
- Volume validation respects the security's minimum, maximum and step constraints before sending any order.
- All decisions are driven by best bid/ask updates from Level1 data, matching the tick-based logic of the MQL version.
