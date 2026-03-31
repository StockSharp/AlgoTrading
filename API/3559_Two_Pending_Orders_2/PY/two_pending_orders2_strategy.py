import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class two_pending_orders2_strategy(Strategy):
    def __init__(self):
        super(two_pending_orders2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._lookback = self.Param("Lookback", 10)

        self._highs = []
        self._lows = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    def OnReseted(self):
        super(two_pending_orders2_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(two_pending_orders2_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self.Lookback

        if len(self._highs) < lookback:
            self._enqueue_candle(candle)
            return

        highest = max(self._highs)
        lowest = min(self._lows)
        close = float(candle.ClosePrice)
        range_val = highest - lowest
        breakout_padding = range_val * 0.05

        if close > highest + breakout_padding:
            if self.Position <= 0:
                self.BuyMarket()
        elif close < lowest - breakout_padding:
            if self.Position >= 0:
                self.SellMarket()

        self._enqueue_candle(candle)

    def _enqueue_candle(self, candle):
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        lookback = self.Lookback
        while len(self._highs) > lookback:
            self._highs.pop(0)
            self._lows.pop(0)

    def CreateClone(self):
        return two_pending_orders2_strategy()
