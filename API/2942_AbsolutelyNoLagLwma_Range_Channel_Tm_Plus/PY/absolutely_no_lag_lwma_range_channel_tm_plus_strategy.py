import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class absolutely_no_lag_lwma_range_channel_tm_plus_strategy(Strategy):
    def __init__(self):
        super(absolutely_no_lag_lwma_range_channel_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast WMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow WMA period", "Indicators")

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(absolutely_no_lag_lwma_range_channel_tm_plus_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(absolutely_no_lag_lwma_range_channel_tm_plus_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_wma = WeightedMovingAverage()
        fast_wma.Length = self.FastPeriod
        slow_wma = WeightedMovingAverage()
        slow_wma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_wma, slow_wma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_wma)
            self.DrawIndicator(area, slow_wma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        prev_above = self._prev_fast > self._prev_slow
        curr_above = fv > sv

        self._prev_fast = fv
        self._prev_slow = sv

        if not prev_above and curr_above:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif prev_above and not curr_above:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return absolutely_no_lag_lwma_range_channel_tm_plus_strategy()
