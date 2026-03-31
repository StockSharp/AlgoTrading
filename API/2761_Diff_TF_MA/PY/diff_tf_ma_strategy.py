import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class diff_tf_ma_strategy(Strategy):

    def __init__(self):
        super(diff_tf_ma_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._reverse_signals = self.Param("ReverseSignals", False)
        self.Volume = 0.1
        self._base_ma = None
        self._higher_ma = None
        self._higher_ma_last = None
        self._higher_ma_prev = None
        self._base_ma_last = None
        self._base_ma_prev = None

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def HigherCandleType(self):
        return self._higher_candle_type.Value

    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    def OnStarted2(self, time):
        super(diff_tf_ma_strategy, self).OnStarted2(time)
        base_span = self.CandleType.Arg
        higher_span = self.HigherCandleType.Arg
        ratio = higher_span.TotalMinutes / base_span.TotalMinutes
        base_length = max(1, int(self.MaPeriod * ratio))

        self._base_ma = SimpleMovingAverage()
        self._base_ma.Length = base_length
        self._higher_ma = SimpleMovingAverage()
        self._higher_ma.Length = self.MaPeriod

        higher_sub = self.SubscribeCandles(self.HigherCandleType)
        higher_sub.Bind(self._higher_ma, self._process_higher).Start()

        base_sub = self.SubscribeCandles(self.CandleType)
        base_sub.Bind(self._base_ma, self._process_base).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, base_sub)
            self.DrawOwnTrades(area)

    def _process_higher(self, candle, higher_ma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._higher_ma.IsFormed:
            return
        self._higher_ma_prev = self._higher_ma_last
        self._higher_ma_last = float(higher_ma_value)

    def _process_base(self, candle, base_ma_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._base_ma.IsFormed:
            return
        self._base_ma_prev = self._base_ma_last
        self._base_ma_last = float(base_ma_value)

        if self._higher_ma_prev is None or self._higher_ma_last is None:
            return
        if self._base_ma_prev is None or self._base_ma_last is None:
            return

        cross_up = self._higher_ma_prev < self._base_ma_prev and self._higher_ma_last > self._base_ma_last
        cross_down = self._higher_ma_prev > self._base_ma_prev and self._higher_ma_last < self._base_ma_last

        if self.ReverseSignals:
            cross_up, cross_down = cross_down, cross_up

        pos = float(self.Position)
        if cross_up and pos <= 0:
            self.BuyMarket(float(self.Volume) + abs(pos))
        elif cross_down and pos >= 0:
            self.SellMarket(float(self.Volume) + abs(pos))

    def OnReseted(self):
        super(diff_tf_ma_strategy, self).OnReseted()
        self._higher_ma_last = None
        self._higher_ma_prev = None
        self._base_ma_last = None
        self._base_ma_prev = None

    def CreateClone(self):
        return diff_tf_ma_strategy()
