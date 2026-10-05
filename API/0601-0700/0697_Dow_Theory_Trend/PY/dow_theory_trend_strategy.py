import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class dow_theory_trend_strategy(Strategy):
    """
    Dow Theory Trend strategy.
    A pivot high is a candle whose high is above the highs of the PivotLookback candles on each side, a pivot low mirrors it with
    lows; a pivot is confirmed PivotLookback candles after it formed. When the last pivot high is above the one before it and the
    last pivot low is above the one before it, the trend is up and the strategy goes long; lower highs and lower lows go short.
    The opposite signal reverses the position.
    """

    def __init__(self):
        super(dow_theory_trend_strategy, self).__init__()
        self._pivot_lookback = self.Param("PivotLookback", 10).SetGreaterThanZero().SetDisplay("Pivot Lookback", "Candles on each side of a pivot", "Pivots")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._last_pivot_high = None
        self._prev_pivot_high = None
        self._last_pivot_low = None
        self._prev_pivot_low = None

    def OnReseted(self):
        super(dow_theory_trend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dow_theory_trend_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._pivot_lookback.Value
        size = lookback * 2 + 1

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)

        if len(self._highs) > size:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < size:
            return

        center_high = self._highs[lookback]
        center_low = self._lows[lookback]
        is_pivot_high = True
        is_pivot_low = True

        for i in range(size):
            if i == lookback:
                continue
            if self._highs[i] >= center_high:
                is_pivot_high = False
            if self._lows[i] <= center_low:
                is_pivot_low = False

        if is_pivot_high:
            self._prev_pivot_high = self._last_pivot_high
            self._last_pivot_high = center_high

        if is_pivot_low:
            self._prev_pivot_low = self._last_pivot_low
            self._last_pivot_low = center_low

        if self._last_pivot_high is None or self._prev_pivot_high is None or self._last_pivot_low is None or self._prev_pivot_low is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        up_trend = self._last_pivot_high > self._prev_pivot_high and self._last_pivot_low > self._prev_pivot_low
        down_trend = self._last_pivot_high < self._prev_pivot_high and self._last_pivot_low < self._prev_pivot_low

        if up_trend and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif down_trend and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return dow_theory_trend_strategy()
