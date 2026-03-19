import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class two_ma_four_level_strategy(Strategy):
    """Two smoothed MA crossover with four level offsets and StartProtection SL/TP."""
    def __init__(self):
        super(two_ma_four_level_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetGreaterThanZero().SetDisplay("Fast Period", "Fast smoothed MA period", "Moving Averages")
        self._slow_period = self.Param("SlowPeriod", 30).SetGreaterThanZero().SetDisplay("Slow Period", "Slow smoothed MA period", "Moving Averages")
        self._most_top = self.Param("MostTopLevel", 2).SetGreaterThanZero().SetDisplay("Extreme Upper Level", "Highest positive offset", "Levels")
        self._top = self.Param("TopLevel", 1).SetGreaterThanZero().SetDisplay("Upper Level", "Second positive offset", "Levels")
        self._lower = self.Param("LowerLevel", 1).SetGreaterThanZero().SetDisplay("Lower Level", "Second negative offset", "Levels")
        self._lowermost = self.Param("LowermostLevel", 2).SetGreaterThanZero().SetDisplay("Extreme Lower Level", "Largest negative offset", "Levels")
        self._tp = self.Param("TakeProfitPips", 500).SetGreaterThanZero().SetDisplay("Take Profit", "TP distance", "Risk")
        self._sl = self.Param("StopLossPips", 1000).SetGreaterThanZero().SetDisplay("Stop Loss", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(two_ma_four_level_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(two_ma_four_level_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_ma = SmoothedMovingAverage()
        fast_ma.Length = self._fast_period.Value
        slow_ma = SmoothedMovingAverage()
        slow_ma.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ma, slow_ma, self.OnProcess).Start()

        sl = self._sl.Value
        tp = self._tp.Value
        if sl > 0 or tp > 0:
            self.StartProtection(self.CreateProtection(sl if sl > 0 else 0, tp if tp > 0 else 0))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def _is_cross_up(self, prev_fast, fast, prev_slow, slow, offset):
        return prev_fast <= prev_slow + offset and fast > slow + offset

    def _is_cross_down(self, prev_fast, fast, prev_slow, slow, offset):
        return prev_fast >= prev_slow + offset and fast < slow + offset

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        pip = 1.0
        pf = self._prev_fast
        ps = self._prev_slow
        most_top = self._most_top.Value * pip
        top = self._top.Value * pip
        lower = self._lower.Value * pip
        lowermost = self._lowermost.Value * pip

        # Check for cross up at any level
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

    def CreateClone(self):
        return two_ma_four_level_strategy()
