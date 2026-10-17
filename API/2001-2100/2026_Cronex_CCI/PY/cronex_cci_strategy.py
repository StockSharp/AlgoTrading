import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


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
        self._prev_fast = self._prev_slow = None
        self._pending_order = None
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.FastPeriod
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.SlowPeriod
        self.Indicators.Add(self._fast_ma)
        self.Indicators.Add(self._slow_ma)
        self.SubscribeCandles(self.CandleType).Bind(self._cci, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        fast_result = process_float(self._fast_ma, cci_value, candle.OpenTime, True)
        slow_result = process_float(self._slow_ma, cci_value, candle.OpenTime, True)
        if not fast_result.IsFormed or not slow_result.IsFormed:
            return
        fast, slow = float(fast_result), float(slow_result)
        buy = self._prev_fast is not None and self._prev_fast >= self._prev_slow and fast < slow
        sell = self._prev_fast is not None and self._prev_fast <= self._prev_slow and fast > slow
        self._prev_fast, self._prev_slow = fast, slow
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if buy:
            if self.Position < 0 and self.EnableShortExit:
                self._pending_order = self.BuyMarket(-self.Position + (self.Volume if self.EnableLongEntry else 0))
            elif self.Position == 0 and self.EnableLongEntry:
                self._pending_order = self.BuyMarket(self.Volume)
        elif sell:
            if self.Position > 0 and self.EnableLongExit:
                self._pending_order = self.SellMarket(self.Position + (self.Volume if self.EnableShortEntry else 0))
            elif self.Position == 0 and self.EnableShortEntry:
                self._pending_order = self.SellMarket(self.Volume)

    def OnReseted(self):
        super(cronex_cci_strategy, self).OnReseted()
        self._prev_fast = self._prev_slow = None
        self._fast_ma = self._slow_ma = self._cci = self._pending_order = None

    def CreateClone(self):
        return cronex_cci_strategy()
