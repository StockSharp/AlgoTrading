import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adjustable_moving_average_strategy(Strategy):
    """Moving average crossover with adjustable gap filter."""

    def __init__(self):
        super(adjustable_moving_average_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast period", "Short moving average length", "Moving averages")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow period", "Long moving average length", "Moving averages")
        self._min_gap_points = self.Param("MinGapPoints", 3.0) \
            .SetNotNegative() \
            .SetDisplay("Minimum gap (points)", "Required distance between fast and slow MAs", "Trading")
        self._fixed_lot = self.Param("FixedLot", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Fixed lot", "Volume used for orders", "Money management")

        self._point_value = 0.0
        self._min_gap_threshold = 0.0
        self._previous_signal = 0
        self._has_initial_signal = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def MinGapPoints(self):
        return self._min_gap_points.Value

    @property
    def FixedLot(self):
        return self._fixed_lot.Value

    def OnReseted(self):
        super(adjustable_moving_average_strategy, self).OnReseted()
        self._point_value = 0.0
        self._min_gap_threshold = 0.0
        self._previous_signal = 0
        self._has_initial_signal = False

    def OnStarted(self, time):
        super(adjustable_moving_average_strategy, self).OnStarted(time)

        fast_len = min(self.FastPeriod, self.SlowPeriod)
        slow_len = max(self.FastPeriod, self.SlowPeriod)

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = fast_len
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = slow_len

        self._point_value = self._calc_point_value()
        self._min_gap_threshold = float(self.MinGapPoints) * self._point_value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _calc_point_value(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
            if step > 0:
                if step == 0.00001 or step == 0.001:
                    return step * 10.0
                return step
        return 0.0

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast)
        sv = float(slow)

        gap_up = fv - sv
        gap_down = sv - fv

        if not self._has_initial_signal:
            if gap_up >= self._min_gap_threshold:
                self._previous_signal = 1
                self._has_initial_signal = True
            elif gap_down >= self._min_gap_threshold:
                self._previous_signal = -1
                self._has_initial_signal = True
            return

        if self._previous_signal > 0:
            if gap_down >= self._min_gap_threshold:
                self._close_position()
                self.SellMarket(self.FixedLot)
                self._previous_signal = -1
        elif self._previous_signal < 0:
            if gap_up >= self._min_gap_threshold:
                self._close_position()
                self.BuyMarket(self.FixedLot)
                self._previous_signal = 1

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        return adjustable_moving_average_strategy()
