import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker, WeightedMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class cronex_de_marker_crossover_strategy(Strategy):
    """Cronex DeMarker crossover strategy. Smooths the DeMarker oscillator with
    fast and slow WMA and trades on their crossover."""

    def __init__(self):
        super(cronex_de_marker_crossover_strategy, self).__init__()

        self._de_marker_period = self.Param("DeMarkerPeriod", 25) \
            .SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicators")
        self._fast_ma_period = self.Param("FastMaPeriod", 14) \
            .SetDisplay("Fast LWMA Period", "Length of the fast linear weighted moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 25) \
            .SetDisplay("Slow LWMA Period", "Length of the slow linear weighted moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame of processed candles", "General")

        self._de_marker = None
        self._fast_ma = None
        self._slow_ma = None
        self._previous_fast = None
        self._previous_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DeMarkerPeriod(self):
        return self._de_marker_period.Value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    def OnReseted(self):
        super(cronex_de_marker_crossover_strategy, self).OnReseted()
        self._de_marker = None
        self._fast_ma = None
        self._slow_ma = None
        self._previous_fast = None
        self._previous_slow = None

    def OnStarted2(self, time):
        super(cronex_de_marker_crossover_strategy, self).OnStarted2(time)

        self._de_marker = DeMarker()
        self._de_marker.Length = self.DeMarkerPeriod

        self._fast_ma = WeightedMovingAverage()
        self._fast_ma.Length = self.FastMaPeriod

        self._slow_ma = WeightedMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._de_marker is None or self._fast_ma is None or self._slow_ma is None:
            return

        dm_input = CandleIndicatorValue(self._de_marker, candle)
        dm_input.IsFinal = True
        dm_result = self._de_marker.Process(dm_input)
        if dm_result.IsEmpty:
            return
        dm_value = float(dm_result)

        fast_input = DecimalIndicatorValue(self._fast_ma, dm_value, candle.OpenTime)
        fast_input.IsFinal = True
        fast_result = self._fast_ma.Process(fast_input)
        if fast_result.IsEmpty:
            return
        fast_value = float(fast_result)

        slow_input = DecimalIndicatorValue(self._slow_ma, dm_value, candle.OpenTime)
        slow_input.IsFinal = True
        slow_result = self._slow_ma.Process(slow_input)
        if slow_result.IsEmpty:
            return
        slow_value = float(slow_result)

        if not self._de_marker.IsFormed or not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            self._previous_fast = fast_value
            self._previous_slow = slow_value
            return

        previous_fast = self._previous_fast
        previous_slow = self._previous_slow

        self._previous_fast = fast_value
        self._previous_slow = slow_value

        if previous_fast is None or previous_slow is None:
            return

        cross_up = previous_fast <= previous_slow and fast_value > slow_value
        cross_down = previous_fast >= previous_slow and fast_value < slow_value

        if cross_up:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return cronex_de_marker_crossover_strategy()
