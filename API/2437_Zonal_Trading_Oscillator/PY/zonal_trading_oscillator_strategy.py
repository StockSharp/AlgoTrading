import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class zonal_trading_oscillator_strategy(Strategy):
    def __init__(self):
        super(zonal_trading_oscillator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._medians = []
        self._ao_values = []
        self._prev_ao = None
        self._prev_ac = None
        self._ao_trend = 0
        self._ac_trend = 0
        self._last_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _get_average(self, values, length):
        start = len(values) - length
        s = 0.0
        for i in range(start, len(values)):
            s += values[i]
        return s / length

    def OnStarted2(self, time):
        super(zonal_trading_oscillator_strategy, self).OnStarted2(time)

        self._medians = []
        self._ao_values = []
        self._prev_ao = None
        self._prev_ac = None
        self._ao_trend = 0
        self._ac_trend = 0
        self._last_signal = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        self._medians.append(median)
        if len(self._medians) > 34:
            self._medians.pop(0)

        if len(self._medians) < 34:
            return

        ao = self._get_average(self._medians, 5) - self._get_average(self._medians, 34)
        self._ao_values.append(ao)
        if len(self._ao_values) > 5:
            self._ao_values.pop(0)

        if len(self._ao_values) < 5:
            self._prev_ao = ao
            return

        ac = ao - self._get_average(self._ao_values, 5)

        if self._prev_ao is not None and self._prev_ac is not None:
            if ao > self._prev_ao:
                self._ao_trend = 1
            elif ao < self._prev_ao:
                self._ao_trend = -1

            if ac > self._prev_ac:
                self._ac_trend = 1
            elif ac < self._prev_ac:
                self._ac_trend = -1

            if self._ao_trend > 0 and self._ac_trend > 0 and self._last_signal != 1 and self.Position <= 0:
                self.BuyMarket()
                self._last_signal = 1
            elif self._ao_trend < 0 and self._ac_trend < 0 and self._last_signal != -1 and self.Position >= 0:
                self.SellMarket()
                self._last_signal = -1

        self._prev_ao = ao
        self._prev_ac = ac

    def OnReseted(self):
        super(zonal_trading_oscillator_strategy, self).OnReseted()
        self._medians = []
        self._ao_values = []
        self._prev_ao = None
        self._prev_ac = None
        self._ao_trend = 0
        self._ac_trend = 0
        self._last_signal = 0

    def CreateClone(self):
        return zonal_trading_oscillator_strategy()
