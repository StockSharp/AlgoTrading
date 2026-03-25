import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_ma_four_level_strategy(Strategy):
    def __init__(self):
        super(two_ma_four_level_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10)
        self._slow_period = self.Param("SlowPeriod", 30)
        self._most_top = self.Param("MostTopLevel", 2)
        self._top = self.Param("TopLevel", 1)
        self._lower = self.Param("LowerLevel", 1)
        self._lowermost = self.Param("LowermostLevel", 2)
        self._tp = self.Param("TakeProfitPips", 500)
        self._sl = self.Param("StopLossPips", 1000)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(two_ma_four_level_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(two_ma_four_level_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_ma = SmoothedMovingAverage()
        fast_ma.Length = int(self._fast_period.Value)
        slow_ma = SmoothedMovingAverage()
        slow_ma.Length = int(self._slow_period.Value)

        sec = self.Security
        pip = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0

        tp_pips = int(self._tp.Value)
        sl_pips = int(self._sl.Value)
        self.StartProtection(
            Unit(tp_pips * pip, UnitTypes.Absolute),
            Unit(sl_pips * pip, UnitTypes.Absolute))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self.OnProcess).Start()

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        sec = self.Security
        pip = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.05

        pf = self._prev_fast
        ps = self._prev_slow
        most_top = int(self._most_top.Value) * pip
        top = int(self._top.Value) * pip
        lower = int(self._lower.Value) * pip
        lowermost = int(self._lowermost.Value) * pip

        buy_signal = (self._is_cross_up(pf, fv, ps, sv, 0) or
                      self._is_cross_up(pf, fv, ps, sv, most_top) or
                      self._is_cross_up(pf, fv, ps, sv, top) or
                      self._is_cross_up(pf, fv, ps, sv, -lowermost) or
                      self._is_cross_up(pf, fv, ps, sv, -lower))

        sell_signal = (self._is_cross_down(pf, fv, ps, sv, 0) or
                       self._is_cross_down(pf, fv, ps, sv, most_top) or
                       self._is_cross_down(pf, fv, ps, sv, top) or
                       self._is_cross_down(pf, fv, ps, sv, -lowermost) or
                       self._is_cross_down(pf, fv, ps, sv, -lower))

        if buy_signal:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def _is_cross_up(self, prev_fast, fast, prev_slow, slow, offset):
        return prev_fast <= prev_slow + offset and fast > slow + offset

    def _is_cross_down(self, prev_fast, fast, prev_slow, slow, offset):
        return prev_fast >= prev_slow + offset and fast < slow + offset

    def CreateClone(self):
        return two_ma_four_level_strategy()
