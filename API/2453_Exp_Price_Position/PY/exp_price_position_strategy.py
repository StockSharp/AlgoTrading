import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SmoothedMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class exp_price_position_strategy(Strategy):
    """
    Price position with step trend filter based on smoothed moving averages.
    Combines SMMA crossover with price position relative to signal level.
    """

    def __init__(self):
        super(exp_price_position_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 2) \
            .SetDisplay("Fast Period", "Fast SMMA period", "Parameters")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow SMMA period", "Parameters")
        self._median_fast_period = self.Param("MedianFastPeriod", 26) \
            .SetDisplay("Median Fast Period", "Median SMMA period", "Parameters")
        self._median_slow_period = self.Param("MedianSlowPeriod", 20) \
            .SetDisplay("Median Slow Period", "Median SMA period", "Parameters")
        self._tp_sl_ratio = self.Param("TpSlRatio", 3.0) \
            .SetDisplay("TP/SL Ratio", "Take profit to stop loss ratio", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0) \
            .SetDisplay("Trailing Stop", "Trailing stop in points", "Risk")
        self._use_trailing = self.Param("UseTrailingStop", True) \
            .SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_fast = None
        self._prev_slow = None
        self._last_cross_level = 0.0
        self._has_cross_level = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_price_position_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None
        self._last_cross_level = 0.0
        self._has_cross_level = False

    def OnStarted(self, time):
        super(exp_price_position_strategy, self).OnStarted(time)

        if self._use_trailing.Value:
            ts = float(self._trailing_stop_pips.Value)
            ratio = float(self._tp_sl_ratio.Value)
            self.StartProtection(
                Unit(float(ts * ratio), UnitTypes.Absolute),
                Unit(float(ts), UnitTypes.Absolute)
            )

        fast = SmoothedMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SmoothedMovingAverage()
        slow.Length = self._slow_period.Value
        median_fast = SmoothedMovingAverage()
        median_fast.Length = self._median_fast_period.Value
        median_slow = SimpleMovingAverage()
        median_slow.Length = self._median_slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, median_fast, median_slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val, mf_val, ms_val):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_val)
        slow_val = float(slow_val)
        mf_val = float(mf_val)
        ms_val = float(ms_val)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        signal = (mf_val + ms_val) / 2.0

        if open_p <= signal and close > signal:
            self._last_cross_level = float(candle.LowPrice)
            self._has_cross_level = True
        elif open_p >= signal and close < signal:
            self._last_cross_level = float(candle.HighPrice)
            self._has_cross_level = True

        if not self._has_cross_level:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        price_pos = 1 if close > self._last_cross_level else -1
        step_up = fast_val > slow_val and fast_val > self._prev_fast and self._prev_fast > self._prev_slow
        step_down = fast_val < slow_val and fast_val < self._prev_fast and self._prev_fast < self._prev_slow

        if (price_pos > 0 and step_up and close > open_p and
                float(candle.LowPrice) < fast_val and self.Position <= 0):
            self.BuyMarket()
        elif (price_pos < 0 and step_down and close < open_p and
                float(candle.HighPrice) > fast_val and self.Position >= 0):
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return exp_price_position_strategy()
