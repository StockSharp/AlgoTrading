# Ultra MFI Money Management Recount Strategy

## Overview
The **Ultra MFI MMRec Strategy** is a direct port of the MetaTrader 5 expert advisor `Exp_UltraMFI_MMRec`. It combines a multi-step smoothed Money Flow Index (MFI) oscillator with streak-based money management. Two internal counters accumulate how many smoothing layers point upward or downward. Crossovers between these counters generate trade signals, while recent trade outcomes determine whether the next position uses the normal or reduced position size.

## Trading Logic
1. **Base Indicator** – a Money Flow Index with configurable length is calculated on the selected candle type.
2. **Ladder Smoothing** – the MFI value is passed through a ladder of moving averages. Each step increases the smoothing length by a fixed increment. Supported smoothing methods are Simple, Exponential, Smoothed, Linear Weighted and Jurik moving averages (other MT5-specific modes are not available in StockSharp).
3. **Directional Counters** – for every bar the strategy compares the current and previous output of every smoothing step. If the step is rising, the bullish counter increases, otherwise the bearish counter increases. Both counters are smoothed again by a final moving average.
4. **Signal Shift** – trading rules operate on finished bars. A configurable `SignalShift` tells the strategy how many closed candles to look back when comparing the counters, mimicking the MT5 behaviour of using `SignalBar=1`.
5. **Entries and Exits** –
   * If the previous bar showed stronger bulls (`bulls > bears`) and the latest bar shows a crossover to `bulls < bears`, the strategy opens a long position. The same condition also closes any open short position.
   * If the previous bar showed stronger bears and the latest bar flips to `bulls > bears`, the strategy opens a short position and closes any open long position.
   * Optional stop-loss and take-profit (percent based) can be managed through `StartProtection`.
6. **Money Management** – the next order size depends on the latest trade outcomes per direction. After each position is closed the realized PnL is inspected:
   * The strategy stores the most recent `BuyTotalTrigger` buy trades and counts how many were losses. When the count reaches `BuyLossTrigger`, the next buy order uses `ReducedVolume`, otherwise it uses `NormalVolume`.
   * The same logic is applied independently for sell trades with `SellTotalTrigger` and `SellLossTrigger`.

## Parameters
- **CandleType** – instrument data type (time frame) used for signal generation.
- **MfiPeriod** – length of the Money Flow Index oscillator.
- **StepSmoothing / FinalSmoothing** – moving average type for the ladder steps and the final counters.
- **StartLength / StepSize / StepsTotal** – geometry of the smoothing ladder (first length, increment, number of steps).
- **FinalSmoothingLength** – length of the counter smoothing stage.
- **SignalShift** – number of completed bars to look back when evaluating signals.
- **NormalVolume / ReducedVolume** – trade size for normal conditions and after a losing streak.
- **BuyTotalTrigger / BuyLossTrigger** – history depth and loss threshold to switch the next long trade to reduced size.
- **SellTotalTrigger / SellLossTrigger** – analogous settings for short trades.
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – enable or disable entries and exits for each direction.
- **TakeProfitPercent / StopLossPercent** – optional percentage-based protection levels.

## Usage Notes
- The ladder smoothing requires enough historical candles to fill every moving average. Wait until the strategy is fully formed before relying on the signals.
- Because StockSharp does not provide MT5-specific smoothers such as JurX, Parabolic, VIDYA or AMA, the closest supported alternatives are used. Jurik smoothing is a good default that reproduces the original feel of the UltraMFI indicator.
- Money management is based on realized PnL. Ensure your backtests include order execution so the realized PnL updates after every position close.
- This port keeps the behaviour of only entering new positions when the current position is flat. When a reversal signal appears while holding the opposite position, the strategy first exits the existing trade and will enter on the next eligible bar once flat.
