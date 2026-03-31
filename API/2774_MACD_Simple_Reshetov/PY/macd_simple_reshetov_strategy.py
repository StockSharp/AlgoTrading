import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_simple_reshetov_strategy(Strategy):

    def __init__(self):
        super(macd_simple_reshetov_strategy, self).__init__()
        self._df = self.Param("Df", 1)
        self._ds = self.Param("Ds", 2)
        self._signal_period = self.Param("SignalPeriod", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._macd = None

    @property
    def Df(self):
        return self._df.Value

    @property
    def Ds(self):
        return self._ds.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(macd_simple_reshetov_strategy, self).OnStarted2(time)

        fast_period = self.SignalPeriod + self.Df
        slow_period = self.SignalPeriod + self.Ds + self.Df

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = fast_period
        self._macd.Macd.LongMa.Length = slow_period
        self._macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ = CandleIndicatorValue(self._macd, candle)
        civ.IsFinal = True
        result = self._macd.Process(civ)
        if not self._macd.IsFormed:
            return

        try:
            macd_line = float(result.Macd) if result.Macd is not None else 0.0
            signal_line = float(result.Signal) if result.Signal is not None else 0.0
        except:
            return

        pos = float(self.Position)

        if pos > 0:
            if macd_line < 0:
                self.SellMarket(pos)
            return

        if pos < 0:
            if macd_line > 0:
                self.BuyMarket(abs(pos))
            return

        if macd_line * signal_line <= 0:
            return

        if macd_line > 0 and macd_line > signal_line:
            self.BuyMarket(float(self.Volume))
        elif macd_line < 0 and macd_line < signal_line:
            self.SellMarket(float(self.Volume))

    def OnReseted(self):
        super(macd_simple_reshetov_strategy, self).OnReseted()
        self._macd = None

    def CreateClone(self):
        return macd_simple_reshetov_strategy()
