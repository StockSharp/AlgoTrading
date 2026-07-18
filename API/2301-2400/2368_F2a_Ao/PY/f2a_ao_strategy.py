import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class f2a_ao_strategy(Strategy):
    def __init__(self):
        super(f2a_ao_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("AO Fast", "Fast period for Awesome Oscillator", "Awesome Oscillator")
        self._slow_period = self.Param("SlowPeriod", 34) \
            .SetDisplay("AO Slow", "Slow period for Awesome Oscillator", "Awesome Oscillator")
        self._filter_length = self.Param("FilterLength", 3) \
            .SetDisplay("Filter", "SMA length for AO filter", "Awesome Oscillator")
        self._previous_ao = None
        self._previous_filtered_ao = None
        self._bars_since_trade = 20

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def filter_length(self):
        return self._filter_length.Value

    def OnReseted(self):
        super(f2a_ao_strategy, self).OnReseted()
        self._previous_ao = None
        self._previous_filtered_ao = None
        self._bars_since_trade = 20

    def OnStarted2(self, time):
        super(f2a_ao_strategy, self).OnStarted2(time)
        self._previous_ao = None
        self._previous_filtered_ao = None
        self._bars_since_trade = 20
        ao = AwesomeOscillator()
        ao.ShortMa.Length = int(self.fast_period)
        ao.LongMa.Length = int(self.slow_period)
        filt = SimpleMovingAverage()
        filt.Length = int(self.filter_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ao, filt, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ao)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ao_value, filtered_ao):
        if candle.State != CandleStates.Finished:
            return
        ao_value = float(ao_value)
        filtered_ao = float(filtered_ao)
        self._bars_since_trade += 1
        if self._previous_ao is None:
            self._previous_ao = ao_value
            self._previous_filtered_ao = filtered_ao
            return
        crossed_up = self._previous_ao <= 0 and ao_value > 0
        crossed_down = self._previous_ao >= 0 and ao_value < 0
        if self._bars_since_trade >= 10 and filtered_ao > self._previous_filtered_ao and crossed_up and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_trade = 0
        elif self._bars_since_trade >= 10 and filtered_ao < self._previous_filtered_ao and crossed_down and self.Position >= 0:
            self.SellMarket()
            self._bars_since_trade = 0
        self._previous_ao = ao_value
        self._previous_filtered_ao = filtered_ao

    def CreateClone(self):
        return f2a_ao_strategy()
