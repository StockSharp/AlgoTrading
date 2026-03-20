import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class lazy_bot_v1_strategy(Strategy):
    def __init__(self):
        super(lazy_bot_v1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._lookback = self.Param("Lookback", 30)

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
        super(lazy_bot_v1_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted(self, time):
        super(lazy_bot_v1_strategy, self).OnStarted(time)
        self._highs = []
        self._lows = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self.Lookback

        if len(self._highs) >= lookback:
            highest = max(self._highs)
            lowest = min(self._lows)
            close = float(candle.ClosePrice)
            padding = (highest - lowest) * 0.05

            if close > highest + padding:
                if self.Position <= 0:
                    self.BuyMarket()
            elif close < lowest - padding:
                if self.Position >= 0:
                    self.SellMarket()

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        while len(self._highs) > lookback:
            self._highs.pop(0)
            self._lows.pop(0)

    def CreateClone(self):
        return lazy_bot_v1_strategy()
