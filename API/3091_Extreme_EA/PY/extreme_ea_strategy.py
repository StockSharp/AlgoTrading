import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class extreme_ea_strategy(Strategy):
    def __init__(self):
        super(extreme_ea_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 50) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicator")
        self._slow_ma_period = self.Param("SlowMaPeriod", 200) \
            .SetDisplay("Slow MA", "Slow EMA period", "Indicator")
        self._cci_period = self.Param("CciPeriod", 12) \
            .SetDisplay("CCI Period", "CCI lookback period", "Indicator")
        self._cci_upper_level = self.Param("CciUpperLevel", 50.0) \
            .SetDisplay("CCI Upper", "Upper CCI threshold for sell", "Levels")
        self._cci_lower_level = self.Param("CciLowerLevel", -50.0) \
            .SetDisplay("CCI Lower", "Lower CCI threshold for buy", "Levels")

        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_fast2 = 0.0
        self._prev_slow2 = 0.0
        self._has_prev = False

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def cci_upper_level(self):
        return self._cci_upper_level.Value

    @property
    def cci_lower_level(self):
        return self._cci_lower_level.Value

    def OnReseted(self):
        super(extreme_ea_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_fast2 = 0.0
        self._prev_slow2 = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(extreme_ea_strategy, self).OnStarted(time)

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.slow_ma_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._fast_ma, self._slow_ma, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            self._prev_fast2 = self._prev_fast
            self._prev_slow2 = self._prev_slow
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return

        if not self._has_prev:
            self._prev_fast2 = self._prev_fast
            self._prev_slow2 = self._prev_slow
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return

        # Buy: fast crosses above slow
        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        # Sell: fast crosses below slow
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast2 = self._prev_fast
        self._prev_slow2 = self._prev_slow
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return extreme_ea_strategy()
