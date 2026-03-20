import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class macd_diver_and_rsi_strategy(Strategy):
    def __init__(self):
        super(macd_diver_and_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)

        self._prev_histogram = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    def OnReseted(self):
        super(macd_diver_and_rsi_strategy, self).OnReseted()
        self._prev_histogram = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(macd_diver_and_rsi_strategy, self).OnStarted(time)
        self._has_prev = False

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, rsi, self._process_candle).Start()

    def _process_candle(self, candle, macd_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFinal or not rsi_value.IsFinal:
            return

        macd_main = float(macd_value.Macd)
        signal = float(macd_value.Signal)
        histogram = macd_main - signal
        rsi_val = float(rsi_value.ToDecimal())

        if self._has_prev:
            if self._prev_histogram <= 0 and histogram > 0 and self._prev_rsi < 35 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_histogram >= 0 and histogram < 0 and self._prev_rsi > 65 and self.Position >= 0:
                self.SellMarket()

        self._prev_histogram = histogram
        self._prev_rsi = rsi_val
        self._has_prev = True

    def CreateClone(self):
        return macd_diver_and_rsi_strategy()
