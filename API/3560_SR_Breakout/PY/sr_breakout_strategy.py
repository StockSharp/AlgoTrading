import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class sr_breakout_strategy(Strategy):
    def __init__(self):
        super(sr_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._lookback_length = self.Param("LookbackLength", 20)

        self._highs = []
        self._lows = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LookbackLength(self):
        return self._lookback_length.Value

    @LookbackLength.setter
    def LookbackLength(self, value):
        self._lookback_length.Value = value

    def OnReseted(self):
        super(sr_breakout_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted(self, time):
        super(sr_breakout_strategy, self).OnStarted(time)
        self._highs = []
        self._lows = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self.LookbackLength

        if len(self._highs) < lookback:
            self._enqueue_candle(candle)
            return

        upper = max(self._highs)
        lower = min(self._lows)
        close = float(candle.ClosePrice)
        range_val = upper - lower
        breakout_padding = range_val * 0.05

        if close > upper + breakout_padding:
            if self.Position <= 0:
                self.BuyMarket()
        elif close < lower - breakout_padding:
            if self.Position >= 0:
                self.SellMarket()

        self._enqueue_candle(candle)

    def _enqueue_candle(self, candle):
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        lookback = self.LookbackLength
        while len(self._highs) > lookback:
            self._highs.pop(0)
            self._lows.pop(0)

    def CreateClone(self):
        return sr_breakout_strategy()
