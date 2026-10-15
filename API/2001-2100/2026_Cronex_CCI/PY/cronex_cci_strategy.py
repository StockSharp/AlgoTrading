import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cronex_cci_strategy(Strategy):

    def __init__(self):
        super(cronex_cci_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 25) \
            .SetDisplay("CCI Period", "CCI calculation length", "Indicators")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast Period", "Fast smoothing period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 25) \
            .SetDisplay("Slow Period", "Slow smoothing period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading")
        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading")

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value

    @EnableLongEntry.setter
    def EnableLongEntry(self, value):
        self._enable_long_entry.Value = value

    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value

    @EnableShortEntry.setter
    def EnableShortEntry(self, value):
        self._enable_short_entry.Value = value

    @property
    def EnableLongExit(self):
        return self._enable_long_exit.Value

    @EnableLongExit.setter
    def EnableLongExit(self, value):
        self._enable_long_exit.Value = value

    @property
    def EnableShortExit(self):
        return self._enable_short_exit.Value

    @EnableShortExit.setter
    def EnableShortExit(self, value):
        self._enable_short_exit.Value = value

    def OnStarted2(self, time):
        super(cronex_cci_strategy, self).OnStarted2(time)

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastPeriod
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(cci, fast_ma, slow_ma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, cci_value, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow

        if prev_fast is not None and prev_slow is not None:
            if prev_fast > prev_slow:
                if self.EnableShortExit and self.Position < 0:
                    self.BuyMarket()

                if self.EnableLongEntry and fast_val <= slow_val and self.Position <= 0:
                    self.BuyMarket()
            elif prev_fast < prev_slow:
                if self.EnableLongExit and self.Position > 0:
                    self.SellMarket()

                if self.EnableShortEntry and fast_val >= slow_val and self.Position >= 0:
                    self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def OnReseted(self):
        super(cronex_cci_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def CreateClone(self):
        return cronex_cci_strategy()
