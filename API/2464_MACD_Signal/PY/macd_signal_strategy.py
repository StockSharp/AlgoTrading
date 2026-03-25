import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from System import Decimal
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, CandleIndicatorValue, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_signal_strategy(Strategy):
    def __init__(self):
        super(macd_signal_strategy, self).__init__()

        self._take_profit_ticks = self.Param("TakeProfitTicks", 10.0)
        self._trailing_stop_ticks = self.Param("TrailingStopTicks", 25.0)
        self._fast_period = self.Param("FastPeriod", 9)
        self._slow_period = self.Param("SlowPeriod", 15)
        self._signal_period = self.Param("SignalPeriod", 8)
        self._atr_level = self.Param("AtrLevel", 0.01)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._signal_ema = None
        self._atr = None
        self._prev_delta = 0.0
        self._has_prev_delta = False

    @property
    def TakeProfitTicks(self):
        return self._take_profit_ticks.Value

    @TakeProfitTicks.setter
    def TakeProfitTicks(self, value):
        self._take_profit_ticks.Value = value

    @property
    def TrailingStopTicks(self):
        return self._trailing_stop_ticks.Value

    @TrailingStopTicks.setter
    def TrailingStopTicks(self, value):
        self._trailing_stop_ticks.Value = value

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
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    @property
    def AtrLevel(self):
        return self._atr_level.Value

    @AtrLevel.setter
    def AtrLevel(self, value):
        self._atr_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(macd_signal_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod
        self._signal_ema = ExponentialMovingAverage()
        self._signal_ema.Length = self.SignalPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = 14

        self._prev_delta = 0.0
        self._has_prev_delta = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        tp_ticks = float(self.TakeProfitTicks)
        sl_ticks = float(self.TrailingStopTicks)

        tp_unit = Unit(tp_ticks * step, UnitTypes.Absolute)
        sl_unit = Unit(sl_ticks * step, UnitTypes.Absolute) if sl_ticks > 0 else None
        is_trailing = sl_ticks > 0

        self.StartProtection(tp_unit, sl_unit, is_trailing)

    def ProcessCandle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        atr_input = CandleIndicatorValue(self._atr, candle)
        atr_result = self._atr.Process(atr_input)

        fast_f = float(fast_val)
        slow_f = float(slow_val)
        delta_val = fast_f - slow_f

        signal_input = DecimalIndicatorValue(self._signal_ema, Decimal(delta_val), candle.CloseTime)
        signal_input.IsFinal = True
        signal_result = self._signal_ema.Process(signal_input)
        signal_f = float(signal_result)

        if not atr_result.IsFinal or not self._signal_ema.IsFormed:
            return

        delta_val -= signal_f
        atr_val = float(atr_result)
        atr_level = float(self.AtrLevel)
        rr = atr_val * atr_level

        if not self._has_prev_delta:
            self._prev_delta = delta_val
            self._has_prev_delta = True
            return

        prev_delta = self._prev_delta
        self._prev_delta = delta_val

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        pos = float(self.Position)
        vol = float(self.Volume)

        if pos <= 0 and delta_val > rr and prev_delta <= rr:
            self.BuyMarket(vol + abs(pos))
        elif pos >= 0 and delta_val < -rr and prev_delta >= -rr:
            self.SellMarket(vol + abs(pos))
        elif pos > 0 and delta_val < 0:
            self.SellMarket(abs(pos))
        elif pos < 0 and delta_val > 0:
            self.BuyMarket(abs(pos))

    def OnReseted(self):
        super(macd_signal_strategy, self).OnReseted()
        self._signal_ema = None
        self._atr = None
        self._prev_delta = 0.0
        self._has_prev_delta = False

    def CreateClone(self):
        return macd_signal_strategy()
