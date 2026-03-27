import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, AwesomeOscillator, DecimalIndicatorValue, CandleIndicatorValue
)


class chaos_trader_lite_strategy(Strategy):
    """Chaos Trader Lite: Bill Williams three wise men with divergent bars, AO and fractals."""

    def __init__(self):
        super(chaos_trader_lite_strategy, self).__init__()

        self._magnitude_pips = self.Param("MagnitudePips", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Magnitude", "Distance from lips in pips", "General")
        self._lips_shift = self.Param("LipsShift", 3) \
            .SetDisplay("Lips Shift", "Shift applied to Alligator lips", "Alligator")
        self._teeth_shift = self.Param("TeethShift", 5) \
            .SetDisplay("Teeth Shift", "Shift applied to Alligator teeth", "Alligator")
        self._use_first = self.Param("UseFirstWiseMan", True) \
            .SetDisplay("First Wise Man", "Enable divergent bar setup", "General")
        self._use_second = self.Param("UseSecondWiseMan", True) \
            .SetDisplay("Second Wise Man", "Enable Awesome Oscillator setup", "General")
        self._use_third = self.Param("UseThirdWiseMan", True) \
            .SetDisplay("Third Wise Man", "Enable fractal breakout setup", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._bars = [None] * 5  # bar0..bar4, each = (o, h, lo, c)
        self._lips_queue = []
        self._teeth_queue = []
        self._lips0 = None
        self._teeth0 = None
        self._teeth1 = None
        self._ao_hist = [None] * 6  # ao0..ao5
        self._long_sl = None
        self._short_sl = None
        self._pending_buy = None
        self._pending_sell = None
        self._pending_buy_stop = None
        self._pending_sell_stop = None

    @property
    def MagnitudePips(self):
        return int(self._magnitude_pips.Value)
    @property
    def LipsShift(self):
        return int(self._lips_shift.Value)
    @property
    def TeethShift(self):
        return int(self._teeth_shift.Value)
    @property
    def UseFirstWiseMan(self):
        return self._use_first.Value
    @property
    def UseSecondWiseMan(self):
        return self._use_second.Value
    @property
    def UseThirdWiseMan(self):
        return self._use_third.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(chaos_trader_lite_strategy, self).OnStarted(time)

        self._bars = [None] * 5
        self._lips_queue = []
        self._teeth_queue = []
        self._lips0 = None
        self._teeth0 = None
        self._teeth1 = None
        self._ao_hist = [None] * 6
        self._long_sl = None
        self._short_sl = None
        self._pending_buy = None
        self._pending_sell = None
        self._pending_buy_stop = None
        self._pending_sell_stop = None

        self._lips_sma = SimpleMovingAverage()
        self._lips_sma.Length = 5
        self._teeth_sma = SimpleMovingAverage()
        self._teeth_sma.Length = 8
        self._ao = AwesomeOscillator()
        self._ao.ShortMa.Length = 5
        self._ao.LongMa.Length = 34

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)

        # Check pending entries
        self._check_pending(candle)

        # Update bar history
        self._bars[4] = self._bars[3]
        self._bars[3] = self._bars[2]
        self._bars[2] = self._bars[1]
        self._bars[1] = self._bars[0]
        self._bars[0] = (o, h, lo, c)

        median = (h + lo) / 2.0

        # Lips
        lips_iv = DecimalIndicatorValue(self._lips_sma, Decimal(median), candle.ServerTime)
        lips_iv.IsFinal = True
        lips_val = self._lips_sma.Process(lips_iv)
        if lips_val.IsFinal:
            lv = float(lips_val.Value)
            self._lips_queue.append(lv)
            if len(self._lips_queue) > self.LipsShift:
                self._lips0 = self._lips_queue.pop(0)

        # Teeth
        teeth_iv = DecimalIndicatorValue(self._teeth_sma, Decimal(median), candle.ServerTime)
        teeth_iv.IsFinal = True
        teeth_val = self._teeth_sma.Process(teeth_iv)
        if teeth_val.IsFinal:
            tv = float(teeth_val.Value)
            self._teeth_queue.append(tv)
            if len(self._teeth_queue) > self.TeethShift:
                self._teeth1 = self._teeth0
                self._teeth0 = self._teeth_queue.pop(0)

        # AO
        ao_val = self._ao.Process(CandleIndicatorValue(self._ao, candle))
        if ao_val.IsFinal:
            av = float(ao_val.Value)
            self._ao_hist[5] = self._ao_hist[4]
            self._ao_hist[4] = self._ao_hist[3]
            self._ao_hist[3] = self._ao_hist[2]
            self._ao_hist[2] = self._ao_hist[1]
            self._ao_hist[1] = self._ao_hist[0]
            self._ao_hist[0] = av

        up_fractal = self._get_up_fractal()
        down_fractal = self._get_down_fractal()

        if self._lips_sma.IsFormed and self._teeth_sma.IsFormed:
            self._evaluate_signals(candle, up_fractal, down_fractal)

        self._update_protection(candle)

    def _check_pending(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self._pending_buy is not None and h >= self._pending_buy:
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                    self._short_sl = None
                self.BuyMarket()
                self._long_sl = self._pending_buy_stop
            self._pending_buy = None
            self._pending_buy_stop = None

        if self._pending_sell is not None and lo <= self._pending_sell:
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                    self._long_sl = None
                self.SellMarket()
                self._short_sl = self._pending_sell_stop
            self._pending_sell = None
            self._pending_sell_stop = None

    def _evaluate_signals(self, candle, up_fractal, down_fractal):
        cur = self._bars[0]
        prev = self._bars[1]
        if cur is None or prev is None:
            return

        sec = self.Security
        point = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        mag_thresh = self.MagnitudePips * point

        if self.UseFirstWiseMan and self._lips0 is not None:
            lips = self._lips0
            if self._is_bull_div(cur, prev):
                dist = lips - cur[1]
                if dist > mag_thresh:
                    self._place_buy(cur, point)
            if self._is_bear_div(cur, prev):
                dist = cur[2] - lips
                if dist > mag_thresh:
                    self._place_sell(cur, point)

        if self.UseSecondWiseMan:
            ao1 = self._ao_hist[1]
            ao2 = self._ao_hist[2]
            ao3 = self._ao_hist[3]
            ao4 = self._ao_hist[4]
            ao5 = self._ao_hist[5]
            if ao1 is not None and ao2 is not None and ao3 is not None and ao4 is not None and ao5 is not None:
                if ao1 > ao2 and ao2 > ao3 and ao3 > ao4 and ao4 < ao5:
                    self._place_buy(cur, point)
                if ao1 < ao2 and ao2 < ao3 and ao3 < ao4 and ao4 > ao5:
                    self._place_sell(cur, point)

        if self.UseThirdWiseMan and self._teeth0 is not None:
            teeth = self._teeth0
            offset = self.MagnitudePips * point
            c = float(candle.ClosePrice)
            if up_fractal is not None and c > teeth + offset:
                self._place_buy(cur, point)
            if down_fractal is not None and c < teeth - offset:
                self._place_sell(cur, point)

    def _is_bull_div(self, cur, prev):
        median = (cur[1] + cur[2]) / 2.0
        return cur[2] < prev[2] and cur[3] > median

    def _is_bear_div(self, cur, prev):
        median = (cur[1] + cur[2]) / 2.0
        return cur[1] > prev[1] and cur[3] < median

    def _place_buy(self, bar, point):
        entry = bar[1] + point
        if entry <= 0:
            return
        stop = bar[2] - point
        self._pending_sell = None
        self._pending_sell_stop = None
        self._pending_buy = entry
        self._pending_buy_stop = stop

    def _place_sell(self, bar, point):
        entry = bar[2] - point
        if entry <= 0:
            return
        stop = bar[1] + point
        self._pending_buy = None
        self._pending_buy_stop = None
        self._pending_sell = entry
        self._pending_sell_stop = stop

    def _update_protection(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0 and self._long_sl is not None:
            if lo <= self._long_sl:
                self.SellMarket()
                self._long_sl = None
        elif self.Position < 0 and self._short_sl is not None:
            if h >= self._short_sl:
                self.BuyMarket()
                self._short_sl = None

    def _get_up_fractal(self):
        b = self._bars
        if b[0] is None or b[1] is None or b[2] is None or b[3] is None or b[4] is None:
            return None
        if b[2][1] > b[3][1] and b[2][1] > b[4][1] and b[2][1] > b[1][1] and b[2][1] > b[0][1]:
            return b[2][1]
        return None

    def _get_down_fractal(self):
        b = self._bars
        if b[0] is None or b[1] is None or b[2] is None or b[3] is None or b[4] is None:
            return None
        if b[2][2] < b[3][2] and b[2][2] < b[4][2] and b[2][2] < b[1][2] and b[2][2] < b[0][2]:
            return b[2][2]
        return None

    def OnReseted(self):
        super(chaos_trader_lite_strategy, self).OnReseted()
        self._bars = [None] * 5
        self._lips_queue = []
        self._teeth_queue = []
        self._lips0 = None
        self._teeth0 = None
        self._teeth1 = None
        self._ao_hist = [None] * 6
        self._long_sl = None
        self._short_sl = None
        self._pending_buy = None
        self._pending_sell = None
        self._pending_buy_stop = None
        self._pending_sell_stop = None

    def CreateClone(self):
        return chaos_trader_lite_strategy()
