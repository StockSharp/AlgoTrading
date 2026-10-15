import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class test_macd_strategy(Strategy):
    def __init__(self):
        super(test_macd_strategy, self).__init__()

        self._prev_histogram = None
        self._macd = None

    def OnReseted(self):
        super(test_macd_strategy, self).OnReseted()
        self._prev_histogram = None
        self._macd = None

    def OnStarted2(self, time):
        super(test_macd_strategy, self).OnStarted2(time)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = 12
        self._macd.Macd.LongMa.Length = 26
        self._macd.SignalMa.Length = 9

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.BindEx(self._macd, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed:
            return

        macd_v = float(macd_val.Macd)
        signal_v = float(macd_val.Signal)
        histogram = macd_v - signal_v

        if self._prev_histogram is not None:
            if self._prev_histogram <= 0.0 and histogram > 0.0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_histogram >= 0.0 and histogram < 0.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_histogram = histogram

    def CreateClone(self):
        return test_macd_strategy()
