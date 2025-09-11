# EMA Grid Martingale Cooldown Strategy

Implements an EMA-based long-only grid system with optional martingale sizing and cooldown between grids. A new grid starts when both fast EMAs cross above their slower counterparts. Additional buys are placed at fixed pip intervals, and the position is closed at the weighted average price plus a buffer.
