import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class alligator_trend_strategy(Strategy):
    """
    Classic Bill Williams Alligator strategy with stop management rules.
    Opens long when Lips > Teeth > Jaw, short when reversed.
    Applies break-even, trailing stop, and take-profit logic.
    """

    def __init__(self):
        super(alligator_trend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(240)) \
            .SetDisplay("Candle Type", "Type of candles used for calculations", "General")

        self._jaw_length = self.Param("JawLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Jaw Length", "Smoothed moving average period for the jaw", "Alligator")

        self._teeth_length = self.Param("TeethLength", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Teeth Length", "Smoothed moving average period for the teeth", "Alligator")

        self._lips_length = self.Param("LipsLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lips Length", "Smoothed moving average period for the lips", "Alligator")

        self._jaw_shift = self.Param("JawShift", 8) \
            .SetDisplay("Jaw Shift", "Forward shift applied to the jaw line", "Alligator")

        self._teeth_shift = self.Param("TeethShift", 5) \
            .SetDisplay("Teeth Shift", "Forward shift applied to the teeth line", "Alligator")

        self._lips_shift = self.Param("LipsShift", 3) \
            .SetDisplay("Lips Shift", "Forward shift applied to the lips line", "Alligator")

        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Enable Long", "Allow long entries", "Trading")

        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Enable Short", "Allow short entries", "Trading")

        self._stop_loss_pips = self.Param("StopLossPips", 500.0) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")

        self._take_profit_pips = self.Param("TakeProfitPips", 2000.0) \
            .SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")

        self._zero_level_pips = self.Param("ZeroLevelPips", 300.0) \
            .SetDisplay("Zero Level", "Distance to move stop to break-even", "Risk")

        self._trailing_stop_pips = self.Param("TrailingStopPips", 500.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")

        self._trailing_step_pips = self.Param("TrailingStepPips", 100.0) \
            .SetDisplay("Trailing Step", "Minimum trailing stop increment in pips", "Risk")

        self._jaw_buffer = []
        self._teeth_buffer = []
        self._lips_buffer = []
        self._entry_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._long_breakeven = False
        self._long_best = 0.0
        self._short_stop = None
        self._short_take = None
        self._short_breakeven = False
        self._short_best = 0.0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def JawLength(self): return self._jaw_length.Value
    @JawLength.setter
    def JawLength(self, v): self._jaw_length.Value = v
    @property
    def TeethLength(self): return self._teeth_length.Value
    @TeethLength.setter
    def TeethLength(self, v): self._teeth_length.Value = v
    @property
    def LipsLength(self): return self._lips_length.Value
    @LipsLength.setter
    def LipsLength(self, v): self._lips_length.Value = v
    @property
    def JawShift(self): return self._jaw_shift.Value
    @JawShift.setter
    def JawShift(self, v): self._jaw_shift.Value = v
    @property
    def TeethShift(self): return self._teeth_shift.Value
    @TeethShift.setter
    def TeethShift(self, v): self._teeth_shift.Value = v
    @property
    def LipsShift(self): return self._lips_shift.Value
    @LipsShift.setter
    def LipsShift(self, v): self._lips_shift.Value = v
    @property
    def EnableLong(self): return self._enable_long.Value
    @EnableLong.setter
    def EnableLong(self, v): self._enable_long.Value = v
    @property
    def EnableShort(self): return self._enable_short.Value
    @EnableShort.setter
    def EnableShort(self, v): self._enable_short.Value = v
    @property
    def StopLossPips(self): return self._stop_loss_pips.Value
    @StopLossPips.setter
    def StopLossPips(self, v): self._stop_loss_pips.Value = v
    @property
    def TakeProfitPips(self): return self._take_profit_pips.Value
    @TakeProfitPips.setter
    def TakeProfitPips(self, v): self._take_profit_pips.Value = v
    @property
    def ZeroLevelPips(self): return self._zero_level_pips.Value
    @ZeroLevelPips.setter
    def ZeroLevelPips(self, v): self._zero_level_pips.Value = v
    @property
    def TrailingStopPips(self): return self._trailing_stop_pips.Value
    @TrailingStopPips.setter
    def TrailingStopPips(self, v): self._trailing_stop_pips.Value = v
    @property
    def TrailingStepPips(self): return self._trailing_step_pips.Value
    @TrailingStepPips.setter
    def TrailingStepPips(self, v): self._trailing_step_pips.Value = v

    def OnReseted(self):
        super(alligator_trend_strategy, self).OnReseted()
        self._jaw_buffer = []
        self._teeth_buffer = []
        self._lips_buffer = []
        self._entry_price = 0.0
        self._reset_long()
        self._reset_short()

    def OnStarted(self, time):
        super(alligator_trend_strategy, self).OnStarted(time)

        jaw = SmoothedMovingAverage()
        jaw.Length = self.JawLength
        teeth = SmoothedMovingAverage()
        teeth.Length = self.TeethLength
        lips = SmoothedMovingAverage()
        lips.Length = self.LipsLength

        self._jaw_ind = jaw
        self._teeth_ind = teeth
        self._lips_ind = lips

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jaw)
            self.DrawIndicator(area, teeth)
            self.DrawIndicator(area, lips)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0

        jaw_val = float(process_float(self._jaw_ind, median, candle.CloseTime, True))
        teeth_val = float(process_float(self._teeth_ind, median, candle.CloseTime, True))
        lips_val = float(process_float(self._lips_ind, median, candle.CloseTime, True))

        if not self._jaw_ind.IsFormed or not self._teeth_ind.IsFormed or not self._lips_ind.IsFormed:
            return

        jaw_shifted = self._get_shifted(self._jaw_buffer, jaw_val, self.JawShift)
        teeth_shifted = self._get_shifted(self._teeth_buffer, teeth_val, self.TeethShift)
        lips_shifted = self._get_shifted(self._lips_buffer, lips_val, self.LipsShift)

        if jaw_shifted is None or teeth_shifted is None or lips_shifted is None:
            return

        if self._manage_position(candle):
            return

        bullish = lips_shifted > teeth_shifted and teeth_shifted > jaw_shifted
        bearish = lips_shifted < teeth_shifted and teeth_shifted < jaw_shifted

        if self.Position == 0:
            if bullish and self.EnableLong:
                self.BuyMarket()
            elif bearish and self.EnableShort:
                self.SellMarket()

    def _manage_position(self, candle):
        if self.Position > 0:
            if self._entry_price == 0:
                return False
            self._long_best = max(self._long_best, float(candle.HighPrice))
            if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
                self.SellMarket()
                self._reset_long()
                return True
            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
                self.SellMarket()
                self._reset_long()
                return True
            if self.ZeroLevelPips > 0 and not self._long_breakeven and self._long_stop is not None:
                zero_dist = self._get_price_by_pips(self.ZeroLevelPips)
                if self._long_best - self._entry_price >= zero_dist:
                    self._long_stop = self._entry_price
                    self._long_breakeven = True
            if self.TrailingStopPips > 0:
                trail_dist = self._get_price_by_pips(self.TrailingStopPips)
                step = self._get_price_by_pips(self.TrailingStepPips)
                candidate = self._long_best - trail_dist
                if self._long_stop is None or candidate - self._long_stop >= step:
                    self._long_stop = candidate
        elif self.Position < 0:
            if self._entry_price == 0:
                return False
            self._short_best = min(self._short_best, float(candle.LowPrice)) if self._short_best > 0 else float(candle.LowPrice)
            if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
                self.BuyMarket()
                self._reset_short()
                return True
            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
                self.BuyMarket()
                self._reset_short()
                return True
            if self.ZeroLevelPips > 0 and not self._short_breakeven and self._short_stop is not None:
                zero_dist = self._get_price_by_pips(self.ZeroLevelPips)
                if self._entry_price - float(candle.LowPrice) >= zero_dist:
                    self._short_stop = self._entry_price
                    self._short_breakeven = True
            if self.TrailingStopPips > 0:
                trail_dist = self._get_price_by_pips(self.TrailingStopPips)
                step = self._get_price_by_pips(self.TrailingStepPips)
                candidate = self._short_best + trail_dist
                if self._short_stop is None or self._short_stop - candidate >= step:
                    self._short_stop = candidate
        else:
            self._reset_long()
            self._reset_short()
        return False

    def OnOwnTradeReceived(self, trade):
        super(alligator_trend_strategy, self).OnOwnTradeReceived(trade)
        price = float(trade.Trade.Price)
        direction = trade.Order.Side
        dist_stop = self._get_price_by_pips(self.StopLossPips) if self.StopLossPips > 0 else None
        dist_take = self._get_price_by_pips(self.TakeProfitPips) if self.TakeProfitPips > 0 else None

        if direction == Sides.Buy:
            if self.Position > 0:
                self._entry_price = price
                self._long_stop = price - dist_stop if dist_stop else None
                self._long_take = price + dist_take if dist_take else None
                self._long_breakeven = False
                self._long_best = price
            elif self.Position == 0:
                self._reset_short()
        elif direction == Sides.Sell:
            if self.Position < 0:
                self._entry_price = price
                self._short_stop = price + dist_stop if dist_stop else None
                self._short_take = price - dist_take if dist_take else None
                self._short_breakeven = False
                self._short_best = price
            elif self.Position == 0:
                self._reset_long()

        if self.Position == 0:
            self._reset_long()
            self._reset_short()

    def _get_shifted(self, buffer, value, shift):
        if shift <= 0:
            return value
        buffer.append(value)
        if len(buffer) <= shift:
            return None
        result = buffer[0]
        buffer.pop(0)
        return result

    def _get_price_by_pips(self, pips):
        if pips <= 0:
            return 0.0
        return pips * 1.0

    def _reset_long(self):
        self._long_stop = None
        self._long_take = None
        self._long_breakeven = False
        self._long_best = 0.0

    def _reset_short(self):
        self._short_stop = None
        self._short_take = None
        self._short_breakeven = False
        self._short_best = 0.0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alligator_trend_strategy()
