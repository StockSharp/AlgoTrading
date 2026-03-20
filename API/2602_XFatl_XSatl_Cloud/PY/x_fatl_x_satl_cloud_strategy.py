import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage, JurikMovingAverage)
from StockSharp.Algo.Strategies import Strategy

SMOOTH_SMA = 1
SMOOTH_EMA = 2
SMOOTH_SMMA = 3
SMOOTH_WMA = 4
SMOOTH_JURIK = 5


class x_fatl_x_satl_cloud_strategy(Strategy):
    def __init__(self):
        super(x_fatl_x_satl_cloud_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._fast_method = self.Param("FastMethod", SMOOTH_EMA)
        self._fast_length = self.Param("FastLength", 3)
        self._fast_phase = self.Param("FastPhase", 15)
        self._slow_method = self.Param("SlowMethod", SMOOTH_EMA)
        self._slow_length = self.Param("SlowLength", 5)
        self._slow_phase = self.Param("SlowPhase", 15)
        self._signal_bar = self.Param("SignalBar", 1)
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._allow_long_entry = self.Param("AllowLongEntry", True)
        self._allow_short_entry = self.Param("AllowShortEntry", True)
        self._allow_long_exit = self.Param("AllowLongExit", True)
        self._allow_short_exit = self.Param("AllowShortExit", True)
        self._take_profit_ticks = self.Param("TakeProfitTicks", 2000)
        self._stop_loss_ticks = self.Param("StopLossTicks", 1000)

        self._fast_history = []
        self._slow_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastMethod(self):
        return self._fast_method.Value

    @FastMethod.setter
    def FastMethod(self, value):
        self._fast_method.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def FastPhase(self):
        return self._fast_phase.Value

    @FastPhase.setter
    def FastPhase(self, value):
        self._fast_phase.Value = value

    @property
    def SlowMethod(self):
        return self._slow_method.Value

    @SlowMethod.setter
    def SlowMethod(self, value):
        self._slow_method.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def SlowPhase(self):
        return self._slow_phase.Value

    @SlowPhase.setter
    def SlowPhase(self, value):
        self._slow_phase.Value = value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @SignalBar.setter
    def SignalBar(self, value):
        self._signal_bar.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

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
    def TakeProfitTicks(self):
        return self._take_profit_ticks.Value

    @TakeProfitTicks.setter
    def TakeProfitTicks(self, value):
        self._take_profit_ticks.Value = value

    @property
    def StopLossTicks(self):
        return self._stop_loss_ticks.Value

    @StopLossTicks.setter
    def StopLossTicks(self, value):
        self._stop_loss_ticks.Value = value

    def _create_indicator(self, method, length):
        m = int(method)
        if m == SMOOTH_SMA:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_EMA:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_SMMA:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif m == SMOOTH_WMA:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = JurikMovingAverage()
            ind.Length = length
            return ind

    def OnStarted(self, time):
        super(x_fatl_x_satl_cloud_strategy, self).OnStarted(time)

        self._fast_history = []
        self._slow_history = []

        fast_ind = self._create_indicator(self.FastMethod, int(self.FastLength))
        slow_ind = self._create_indicator(self.SlowMethod, int(self.SlowLength))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ind, slow_ind, self.ProcessCandle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        tp = int(self.TakeProfitTicks)
        sl = int(self.StopLossTicks)

        tp_unit = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else None
        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)

        self._update_history(self._fast_history, fv)
        self._update_history(self._slow_history, sv)

        signal_bar = int(self.SignalBar)
        required = signal_bar + 2

        if len(self._fast_history) < required or len(self._slow_history) < required:
            return

        fast_current = self._get_shifted(self._fast_history, signal_bar)
        fast_previous = self._get_shifted(self._fast_history, signal_bar + 1)
        slow_current = self._get_shifted(self._slow_history, signal_bar)
        slow_previous = self._get_shifted(self._slow_history, signal_bar + 1)

        fast_was_above = fast_previous > slow_previous
        fast_was_below = fast_previous < slow_previous

        close_short = self.AllowShortExit and fast_was_above and self.Position < 0
        if close_short:
            self.BuyMarket()

        close_long = self.AllowLongExit and fast_was_below and self.Position > 0
        if close_long:
            self.SellMarket()

        enter_long = self.AllowLongEntry and fast_was_above and fast_current <= slow_current
        enter_short = self.AllowShortEntry and fast_was_below and fast_current >= slow_current

        if self.Position != 0:
            return

        if enter_long:
            self.BuyMarket()
        elif enter_short:
            self.SellMarket()

    def _update_history(self, history, value):
        history.append(value)
        max_size = int(self.SignalBar) + 2
        while len(history) > max_size:
            history.pop(0)

    def _get_shifted(self, history, shift):
        index = len(history) - shift - 1
        if 0 <= index < len(history):
            return history[index]
        return 0.0

    def OnReseted(self):
        super(x_fatl_x_satl_cloud_strategy, self).OnReseted()
        self._fast_history = []
        self._slow_history = []

    def CreateClone(self):
        return x_fatl_x_satl_cloud_strategy()
