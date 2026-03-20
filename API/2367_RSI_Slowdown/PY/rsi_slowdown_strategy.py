import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_slowdown_strategy(Strategy):
    def __init__(self):
        super(rsi_slowdown_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 2) \
            .SetDisplay("RSI Period", "RSI calculation period", "RSI")
        self._level_max = self.Param("LevelMax", 90.0) \
            .SetDisplay("Upper Level", "Overbought RSI level", "RSI")
        self._level_min = self.Param("LevelMin", 10.0) \
            .SetDisplay("Lower Level", "Oversold RSI level", "RSI")
        self._seek_slowdown = self.Param("SeekSlowdown", True) \
            .SetDisplay("Seek Slowdown", "Check RSI change below 1", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")
        self._previous_rsi = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def level_max(self):
        return self._level_max.Value

    @property
    def level_min(self):
        return self._level_min.Value

    @property
    def seek_slowdown(self):
        return self._seek_slowdown.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_slowdown_strategy, self).OnReseted()
        self._previous_rsi = None

    def OnStarted(self, time):
        super(rsi_slowdown_strategy, self).OnStarted(time)
        self._previous_rsi = None
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self.rsi_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        if self._previous_rsi is None:
            self._previous_rsi = rsi_value
            return
        is_slowdown = not self.seek_slowdown or abs(self._previous_rsi - rsi_value) < 1.0
        lmax = float(self.level_max)
        lmin = float(self.level_min)
        if is_slowdown:
            if rsi_value >= lmax and self.Position <= 0:
                self.BuyMarket()
            elif rsi_value <= lmin and self.Position >= 0:
                self.SellMarket()
        self._previous_rsi = rsi_value

    def CreateClone(self):
        return rsi_slowdown_strategy()
