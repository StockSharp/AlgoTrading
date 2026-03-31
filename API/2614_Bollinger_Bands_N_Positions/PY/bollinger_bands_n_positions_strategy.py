import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import BollingerBands


class bollinger_bands_n_positions_strategy(Strategy):
    """BB breakout strategy with N-position control, SL/TP and trailing stop."""

    def __init__(self):
        super(bollinger_bands_n_positions_strategy, self).__init__()

        self._max_positions = self.Param("MaxPositions", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Positions", "Net position limit in multiples of Volume", "Risk")

        self._bb_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Moving average length", "Indicators")

        self._bb_width = self.Param("BollingerWidth", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators")

        self._sl_pips = self.Param("StopLossPips", 50.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")

        self._tp_pips = self.Param("TakeProfitPips", 50.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")

        self._trail_pips = self.Param("TrailingStopPips", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Trailing Stop (pips)", "Trailing-stop distance in pips", "Risk")

        self._trail_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Trailing Step (pips)", "Trailing adjustment increment", "Risk")

        self._vol_tol = self.Param("VolumeTolerance", 0.00000001) \
            .SetNotNegative() \
            .SetDisplay("Volume Tolerance", "Minimum net position magnitude treated as flat", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")

        self._long_entry = None
        self._short_entry = None
        self._long_trail = None
        self._short_trail = None

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @property
    def BollingerPeriod(self):
        return self._bb_period.Value

    @property
    def BollingerWidth(self):
        return self._bb_width.Value

    @property
    def StopLossPips(self):
        return self._sl_pips.Value

    @property
    def TakeProfitPips(self):
        return self._tp_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trail_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trail_step_pips.Value

    @property
    def VolumeTolerance(self):
        return self._vol_tol.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _step(self):
        sec = self.Security
        s = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        return s

    def OnStarted2(self, time):
        super(bollinger_bands_n_positions_strategy, self).OnStarted2(time)

        bb = BollingerBands()
        bb.Length = self.BollingerPeriod
        bb.Width = self.BollingerWidth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, self.process_candle).Start()

    def process_candle(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        upper = float(bb_val.UpBand) if bb_val.UpBand is not None else 0.0
        lower = float(bb_val.LowBand) if bb_val.LowBand is not None else 0.0

        if self._handle_active(candle):
            return

        if not self.IsFormed:
            return

        self._try_long(candle, upper)
        self._try_short(candle, lower)

    def _handle_active(self, candle):
        tol = float(self.VolumeTolerance)
        if self.Position > tol:
            return self._manage_long(candle)
        if self.Position < -tol:
            return self._manage_short(candle)
        if self._long_entry is not None or self._short_entry is not None:
            self._reset_long()
            self._reset_short()
        return False

    def _manage_long(self, candle):
        if self._long_entry is None:
            self._long_entry = float(candle.ClosePrice)
        entry = self._long_entry
        step = self._step()
        sl = float(self.StopLossPips)
        tp = float(self.TakeProfitPips)
        ts = float(self.TrailingStopPips)
        tstp = float(self.TrailingStepPips)

        if sl > 0:
            if float(candle.LowPrice) <= entry - sl * step:
                self.SellMarket()
                self._reset_long()
                return True
        if tp > 0:
            if float(candle.HighPrice) >= entry + tp * step:
                self.SellMarket()
                self._reset_long()
                return True
        if ts > 0 and tstp > 0:
            td = ts * step
            tstep = tstp * step
            act = td + tstep
            if float(candle.ClosePrice) - entry > act:
                cand = float(candle.ClosePrice) - td
                if self._long_trail is None or cand - self._long_trail > tstep:
                    self._long_trail = cand
            if self._long_trail is not None and float(candle.LowPrice) <= self._long_trail:
                self.SellMarket()
                self._reset_long()
                return True
        return False

    def _manage_short(self, candle):
        if self._short_entry is None:
            self._short_entry = float(candle.ClosePrice)
        entry = self._short_entry
        step = self._step()
        sl = float(self.StopLossPips)
        tp = float(self.TakeProfitPips)
        ts = float(self.TrailingStopPips)
        tstp = float(self.TrailingStepPips)

        if sl > 0:
            if float(candle.HighPrice) >= entry + sl * step:
                self.BuyMarket()
                self._reset_short()
                return True
        if tp > 0:
            if float(candle.LowPrice) <= entry - tp * step:
                self.BuyMarket()
                self._reset_short()
                return True
        if ts > 0 and tstp > 0:
            td = ts * step
            tstep = tstp * step
            act = td + tstep
            if entry - float(candle.ClosePrice) > act:
                cand = float(candle.ClosePrice) + td
                if self._short_trail is None or self._short_trail - cand > tstep:
                    self._short_trail = cand
            if self._short_trail is not None and float(candle.HighPrice) >= self._short_trail:
                self.BuyMarket()
                self._reset_short()
                return True
        return False

    def _try_long(self, candle, upper):
        if float(candle.ClosePrice) <= upper:
            return
        if not self._has_capacity():
            return
        tol = float(self.VolumeTolerance)
        if self.Position < -tol:
            self.BuyMarket()
            self._reset_short()
            return
        if self.Position > tol:
            self.SellMarket()
            self._reset_long()
            return
        self.BuyMarket()
        self._long_entry = float(candle.ClosePrice)
        self._long_trail = None
        self._reset_short()

    def _try_short(self, candle, lower):
        if float(candle.ClosePrice) >= lower:
            return
        if not self._has_capacity():
            return
        tol = float(self.VolumeTolerance)
        if self.Position > tol:
            self.SellMarket()
            self._reset_long()
            return
        if self.Position < -tol:
            self.BuyMarket()
            self._reset_short()
            return
        self.SellMarket()
        self._short_entry = float(candle.ClosePrice)
        self._short_trail = None
        self._reset_long()

    def _has_capacity(self):
        if self.Volume <= 0 or self.MaxPositions <= 0:
            return False
        limit = self.MaxPositions * self.Volume
        return abs(self.Position) < limit - float(self.VolumeTolerance)

    def _reset_long(self):
        self._long_entry = None
        self._long_trail = None

    def _reset_short(self):
        self._short_entry = None
        self._short_trail = None

    def OnReseted(self):
        super(bollinger_bands_n_positions_strategy, self).OnReseted()
        self._reset_long()
        self._reset_short()

    def CreateClone(self):
        return bollinger_bands_n_positions_strategy()
