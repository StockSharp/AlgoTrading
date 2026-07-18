import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
import math


class exp_mama_strategy(Strategy):
    def __init__(self):
        super(exp_mama_strategy, self).__init__()
        self._fast_limit = self.Param("FastLimit", 0.5) \
            .SetDisplay("Fast Limit", "Fast alpha limit", "Indicators")
        self._slow_limit = self.Param("SlowLimit", 0.05) \
            .SetDisplay("Slow Limit", "Slow alpha limit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_mama = None
        self._prev_fama = None
        # MAMA calculator state
        self._p1 = 0.0; self._p2 = 0.0; self._p3 = 0.0
        self._s1 = 0.0; self._s2 = 0.0; self._s3 = 0.0
        self._d1 = 0.0; self._d2 = 0.0; self._d3 = 0.0
        self._q1v = 0.0; self._q2v = 0.0; self._q3v = 0.0
        self._i1v = 0.0; self._i2v = 0.0; self._i3v = 0.0
        self._i21 = 0.0; self._q21 = 0.0
        self._re1 = 0.0; self._im1 = 0.0
        self._phase1 = 0.0; self._period = 0.0
        self._mama_val = None; self._fama_val = None
        self._count = 0

    @property
    def fast_limit(self):
        return self._fast_limit.Value
    @property
    def slow_limit(self):
        return self._slow_limit.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_mama_strategy, self).OnReseted()
        self._prev_mama = None
        self._prev_fama = None
        self._p1 = 0.0; self._p2 = 0.0; self._p3 = 0.0
        self._s1 = 0.0; self._s2 = 0.0; self._s3 = 0.0
        self._d1 = 0.0; self._d2 = 0.0; self._d3 = 0.0
        self._q1v = 0.0; self._q2v = 0.0; self._q3v = 0.0
        self._i1v = 0.0; self._i2v = 0.0; self._i3v = 0.0
        self._i21 = 0.0; self._q21 = 0.0
        self._re1 = 0.0; self._im1 = 0.0
        self._phase1 = 0.0; self._period = 0.0
        self._mama_val = None; self._fama_val = None
        self._count = 0

    def OnStarted2(self, time):
        super(exp_mama_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        result = self._calc_mama(price, float(self.fast_limit), float(self.slow_limit))
        if result is None:
            return
        mama, fama = result
        if self._prev_mama is not None and self._prev_fama is not None:
            was_above = self._prev_mama > self._prev_fama
            is_above = mama > fama
            if was_above and not is_above and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not was_above and is_above and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_mama = mama
        self._prev_fama = fama

    def _calc_mama(self, price, fast, slow):
        self._count += 1
        c0 = 0.0962; c1 = 0.5769; c2 = -0.5769; c3 = -0.0962
        smooth = (4.0 * price + 3.0 * self._p1 + 2.0 * self._p2 + self._p3) / 10.0
        detrender = c0 * smooth + c1 * self._s1 + c2 * self._s2 + c3 * self._s3
        q1 = c0 * detrender + c1 * self._d1 + c2 * self._d2 + c3 * self._d3
        i1 = self._d1
        jI = c0 * i1 + c1 * self._i1v + c2 * self._i2v + c3 * self._i3v
        jQ = c0 * q1 + c1 * self._q1v + c2 * self._q2v + c3 * self._q3v
        i2 = i1 - jQ
        q2 = q1 + jI
        i2 = 0.2 * i2 + 0.8 * self._i21
        q2 = 0.2 * q2 + 0.8 * self._q21
        re = i2 * self._i21 + q2 * self._q21
        im = i2 * self._q21 - q2 * self._i21
        re = 0.2 * re + 0.8 * self._re1
        im = 0.2 * im + 0.8 * self._im1
        period = self._period
        if re != 0.0 and im != 0.0:
            ang = math.atan(im / re)
            if ang != 0.0:
                period = 2.0 * math.pi / ang
        period = min(max(period, 6.0), 50.0)
        phase = 0.0
        if i1 != 0.0:
            phase = math.atan(q1 / i1)
        delta = phase - self._phase1
        if delta < 1.0:
            delta = 1.0
        if delta > 1.5:
            delta = 1.5
        alpha = fast / delta
        if alpha < slow:
            alpha = slow
        mama = price if self._mama_val is None else alpha * price + (1.0 - alpha) * self._mama_val
        fama = price if self._fama_val is None else 0.5 * alpha * mama + (1.0 - 0.5 * alpha) * self._fama_val
        self._p3 = self._p2; self._p2 = self._p1; self._p1 = price
        self._s3 = self._s2; self._s2 = self._s1; self._s1 = smooth
        self._d3 = self._d2; self._d2 = self._d1; self._d1 = detrender
        self._q3v = self._q2v; self._q2v = self._q1v; self._q1v = q1
        self._i3v = self._i2v; self._i2v = self._i1v; self._i1v = i1
        self._i21 = i2; self._q21 = q2
        self._re1 = re; self._im1 = im
        self._phase1 = phase; self._period = period
        self._mama_val = mama; self._fama_val = fama
        if self._count < 7:
            return None
        return (mama, fama)

    def CreateClone(self):
        return exp_mama_strategy()
