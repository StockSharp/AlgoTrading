import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal, Array
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage, JurikMovingAverage)
from StockSharp.Algo.Strategies import Strategy

SMOOTH_SMA = 0
SMOOTH_EMA = 1
SMOOTH_SMMA = 2
SMOOTH_LWMA = 3
SMOOTH_JURIK = 4


class stochastic_chaikins_volatility_strategy(Strategy):
    def __init__(self):
        super(stochastic_chaikins_volatility_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._primary_method = self.Param("PrimaryMethod", SMOOTH_SMA)
        self._primary_length = self.Param("PrimaryLength", 10)
        self._secondary_method = self.Param("SecondaryMethod", SMOOTH_JURIK)
        self._secondary_length = self.Param("SecondaryLength", 5)
        self._stochastic_length = self.Param("StochasticLength", 5)
        self._signal_shift = self.Param("SignalShift", 1)
        self._allow_long_entry = self.Param("AllowLongEntry", True)
        self._allow_short_entry = self.Param("AllowShortEntry", True)
        self._allow_long_exit = self.Param("AllowLongExit", True)
        self._allow_short_exit = self.Param("AllowShortExit", True)
        self._high_level = self.Param("HighLevel", 300.0)
        self._middle_level = self.Param("MiddleLevel", 50.0)
        self._low_level = self.Param("LowLevel", -300.0)

        self._volatility_window = []
        self._main_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PrimaryMethod(self):
        return self._primary_method.Value

    @PrimaryMethod.setter
    def PrimaryMethod(self, value):
        self._primary_method.Value = value

    @property
    def PrimaryLength(self):
        return self._primary_length.Value

    @PrimaryLength.setter
    def PrimaryLength(self, value):
        self._primary_length.Value = value

    @property
    def SecondaryMethod(self):
        return self._secondary_method.Value

    @SecondaryMethod.setter
    def SecondaryMethod(self, value):
        self._secondary_method.Value = value

    @property
    def SecondaryLength(self):
        return self._secondary_length.Value

    @SecondaryLength.setter
    def SecondaryLength(self, value):
        self._secondary_length.Value = value

    @property
    def StochasticLength(self):
        return self._stochastic_length.Value

    @StochasticLength.setter
    def StochasticLength(self, value):
        self._stochastic_length.Value = value

    @property
    def SignalShift(self):
        return self._signal_shift.Value

    @SignalShift.setter
    def SignalShift(self, value):
        self._signal_shift.Value = value

    @property
    def AllowLongEntry(self):
        return self._allow_long_entry.Value

    @AllowLongEntry.setter
    def AllowLongEntry(self, value):
        self._allow_long_entry.Value = value

    @property
    def AllowShortEntry(self):
        return self._allow_short_entry.Value

    @AllowShortEntry.setter
    def AllowShortEntry(self, value):
        self._allow_short_entry.Value = value

    @property
    def AllowLongExit(self):
        return self._allow_long_exit.Value

    @AllowLongExit.setter
    def AllowLongExit(self, value):
        self._allow_long_exit.Value = value

    @property
    def AllowShortExit(self):
        return self._allow_short_exit.Value

    @AllowShortExit.setter
    def AllowShortExit(self, value):
        self._allow_short_exit.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def MiddleLevel(self):
        return self._middle_level.Value

    @MiddleLevel.setter
    def MiddleLevel(self, value):
        self._middle_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    def _create_smoother(self, method, length):
        m = int(method)
        if m == SMOOTH_EMA:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_SMMA:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_LWMA:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_JURIK:
            ind = JurikMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind

    def OnStarted(self, time):
        super(stochastic_chaikins_volatility_strategy, self).OnStarted(time)

        self._primary_smoother = self._create_smoother(self.PrimaryMethod, int(self.PrimaryLength))
        self._secondary_smoother = self._create_smoother(self.SecondaryMethod, int(self.SecondaryLength))

        self._volatility_window = []
        self._main_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        diff = float(candle.HighPrice) - float(candle.LowPrice)

        smoothed_result = self._primary_smoother.Process(self._primary_smoother.CreateValue(candle.OpenTime, Array[object]([Decimal(diff)])))
        if not smoothed_result.IsFormed:
            return

        smoothed_diff = float(smoothed_result)

        stoch_len = int(self.StochasticLength)
        self._volatility_window.append(smoothed_diff)
        while len(self._volatility_window) > stoch_len:
            self._volatility_window.pop(0)

        if len(self._volatility_window) < stoch_len:
            return

        highest = max(self._volatility_window)
        lowest = min(self._volatility_window)

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        if price_step <= 0.0:
            price_step = 0.0001

        range_val = highest - lowest
        denominator = range_val if range_val >= price_step else price_step
        if denominator == 0.0:
            normalized = 0.0
        else:
            normalized = (smoothed_diff - lowest) / denominator

        if normalized < 0.0:
            normalized = 0.0
        elif normalized > 1.0:
            normalized = 1.0

        scaled = normalized * 100.0

        stoch_result = self._secondary_smoother.Process(self._secondary_smoother.CreateValue(candle.OpenTime, Array[object]([Decimal(scaled)])))
        if not stoch_result.IsFormed:
            return

        main = float(stoch_result)
        self._add_history(main)

        signal_shift = int(self.SignalShift)
        min_history = signal_shift + 3

        if len(self._main_history) < min_history:
            return

        idx = signal_shift
        value0 = self._main_history[idx]
        value1 = self._main_history[idx + 1]
        value2 = self._main_history[idx + 2]

        buy_close = self.AllowLongExit and value1 < value2
        sell_close = self.AllowShortExit and value1 > value2
        buy_open = self.AllowLongEntry and value1 > value2 and value0 <= value1
        sell_open = self.AllowShortEntry and value1 < value2 and value0 >= value1

        if self.Position > 0 and buy_close:
            self.SellMarket()
        elif self.Position < 0 and sell_close:
            self.BuyMarket()

        if buy_open and self.Position <= 0:
            self.BuyMarket()
        elif sell_open and self.Position >= 0:
            self.SellMarket()

    def _add_history(self, value):
        self._main_history.insert(0, value)
        max_size = int(self.SignalShift) + 4
        while len(self._main_history) > max_size:
            self._main_history.pop()

    def OnReseted(self):
        super(stochastic_chaikins_volatility_strategy, self).OnReseted()
        self._volatility_window = []
        self._main_history = []

    def CreateClone(self):
        return stochastic_chaikins_volatility_strategy()
