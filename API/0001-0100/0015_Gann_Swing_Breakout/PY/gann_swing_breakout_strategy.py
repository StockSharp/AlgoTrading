import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gann_swing_breakout_strategy(Strategy):
    """
    Gann Swing Breakout.
    A swing high is a candle whose high exceeds the highs of SwingLookback candles on each side, a swing low the reverse.
    A close above the latest swing high while above the SMA opens a long position, a close below the latest swing low
    while below the SMA a short one. A position stays open until the opposing swing is breached.
    """

    def __init__(self):
        super(gann_swing_breakout_strategy, self).__init__()
        self._swing_lookback = self.Param("SwingLookback", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Swing Lookback", "Lookback period for swing high/low", "Trading parameters")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for trend filter MA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._window = []
        self._swing_high = None
        self._swing_low = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gann_swing_breakout_strategy, self).OnReseted()
        self._window = []
        self._swing_high = None
        self._swing_low = None

    def OnStarted2(self, time):
        super(gann_swing_breakout_strategy, self).OnStarted2(time)

        self._window = []
        self._swing_high = None
        self._swing_low = None

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_swings(candle)

        if not ma_value.IsFormed or self._swing_high is None or self._swing_low is None:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = ma_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        position = self.Position

        if close > self._swing_high and close > ma and position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(position))
        elif close < self._swing_low and close < ma and position >= 0:
            self.SellMarket(self.Volume + Math.Abs(position))
        elif position > 0 and close < self._swing_low:
            self.SellMarket(position)
        elif position < 0 and close > self._swing_high:
            self.BuyMarket(-position)

    def _update_swings(self, candle):
        # A pivot is confirmed once SwingLookback candles have closed after it.
        lookback = self._swing_lookback.Value
        self._window.append((candle.HighPrice, candle.LowPrice))
        size = 2 * lookback + 1
        if len(self._window) > size:
            self._window.pop(0)
        if len(self._window) < size:
            return

        pivot_high, pivot_low = self._window[lookback]
        others = self._window[:lookback] + self._window[lookback + 1:]

        if all(pivot_high > high for high, _ in others):
            self._swing_high = pivot_high
        if all(pivot_low < low for _, low in others):
            self._swing_low = pivot_low

    def CreateClone(self):
        return gann_swing_breakout_strategy()
