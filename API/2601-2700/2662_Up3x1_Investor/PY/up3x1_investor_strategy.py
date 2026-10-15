import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class up3x1_investor_strategy(Strategy):
    """Range breakout strategy with SL/TP and trailing stop, based on Up3x1 Investor EA."""

    def __init__(self):
        super(up3x1_investor_strategy, self).__init__()

        self._range_threshold_pips = self.Param("RangeThresholdPips", 2.0) \
            .SetDisplay("Range Threshold (pips)", "Minimum previous candle range in pips", "Signals")
        self._body_threshold_pips = self.Param("BodyThresholdPips", 1.0) \
            .SetDisplay("Body Threshold (pips)", "Minimum previous candle body in pips", "Signals")
        self._stop_loss_pips = self.Param("StopLossPips", 5.0) \
            .SetDisplay("Stop Loss (pips)", "Distance of protective stop in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 5.0) \
            .SetDisplay("Take Profit (pips)", "Distance of profit target in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 3.0) \
            .SetDisplay("Trailing Stop (pips)", "Distance kept behind price when trailing", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step (pips)", "Increment required to move trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for signals", "General")

        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._entry_price = None
        self._highest = 0.0
        self._lowest = 0.0
        self._trailing_stop = None

    @property
    def RangeThresholdPips(self):
        return float(self._range_threshold_pips.Value)
    @property
    def BodyThresholdPips(self):
        return float(self._body_threshold_pips.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def TrailingStopPips(self):
        return float(self._trailing_stop_pips.Value)
    @property
    def TrailingStepPips(self):
        return float(self._trailing_step_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_pip(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 1.0
        return float(sec.PriceStep)

    def OnStarted2(self, time):
        super(up3x1_investor_strategy, self).OnStarted2(time)
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._reset_tracking()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position == 0 and self._entry_price is not None:
            self._reset_tracking()

        pip = self._get_pip()
        sl_dist = self.StopLossPips * pip if self.StopLossPips > 0 else 0.0
        tp_dist = self.TakeProfitPips * pip if self.TakeProfitPips > 0 else 0.0
        trail_dist = self.TrailingStopPips * pip if self.TrailingStopPips > 0 else 0.0
        trail_step = self.TrailingStepPips * pip if self.TrailingStepPips > 0 else 0.0

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)

        # Manage existing position
        if self.Position != 0 and self._entry_price is not None:
            if self._manage_position(candle, sl_dist, tp_dist, trail_dist, trail_step):
                self._prev_open = o
                self._prev_close = c
                self._prev_high = h
                self._prev_low = lo
                self._has_prev = True
                return

        if self.Position != 0:
            self._prev_open = o
            self._prev_close = c
            self._prev_high = h
            self._prev_low = lo
            self._has_prev = True
            return

        ref_o = self._prev_open if self._has_prev else o
        ref_c = self._prev_close if self._has_prev else c
        ref_h = self._prev_high if self._has_prev else h
        ref_lo = self._prev_low if self._has_prev else lo

        rng = ref_h - ref_lo
        body = abs(ref_c - ref_o)
        range_thresh = self.RangeThresholdPips * pip
        body_thresh = self.BodyThresholdPips * pip

        if rng > range_thresh and body > body_thresh and ref_c > ref_o:
            self.BuyMarket()
            self._init_tracking(c)
        elif rng > range_thresh and body > body_thresh and ref_c < ref_o:
            self.SellMarket()
            self._init_tracking(c)

        self._prev_open = o
        self._prev_close = c
        self._prev_high = h
        self._prev_low = lo
        self._has_prev = True

    def _manage_position(self, candle, sl_dist, tp_dist, trail_dist, trail_step):
        if self._entry_price is None:
            return False

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            self._highest = max(self._highest, h)

            if sl_dist > 0 and lo <= self._entry_price - sl_dist:
                self.SellMarket()
                self._reset_tracking()
                return True
            if tp_dist > 0 and h >= self._entry_price + tp_dist:
                self.SellMarket()
                self._reset_tracking()
                return True
            if trail_dist > 0 and self._highest - self._entry_price >= trail_dist + trail_step:
                candidate = self._highest - trail_dist
                if self._trailing_stop is None or candidate - self._trailing_stop >= trail_step:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and lo <= self._trailing_stop:
                self.SellMarket()
                self._reset_tracking()
                return True

        elif self.Position < 0:
            self._lowest = min(self._lowest, lo)

            if sl_dist > 0 and h >= self._entry_price + sl_dist:
                self.BuyMarket()
                self._reset_tracking()
                return True
            if tp_dist > 0 and lo <= self._entry_price - tp_dist:
                self.BuyMarket()
                self._reset_tracking()
                return True
            if trail_dist > 0 and self._entry_price - self._lowest >= trail_dist + trail_step:
                candidate = self._lowest + trail_dist
                if self._trailing_stop is None or self._trailing_stop - candidate >= trail_step:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and h >= self._trailing_stop:
                self.BuyMarket()
                self._reset_tracking()
                return True

        return False

    def _init_tracking(self, entry):
        self._entry_price = entry
        self._highest = entry
        self._lowest = entry
        self._trailing_stop = None

    def _reset_tracking(self):
        self._entry_price = None
        self._highest = 0.0
        self._lowest = 0.0
        self._trailing_stop = None

    def OnReseted(self):
        super(up3x1_investor_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
        self._reset_tracking()

    def CreateClone(self):
        return up3x1_investor_strategy()
