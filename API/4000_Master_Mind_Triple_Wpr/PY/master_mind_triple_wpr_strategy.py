import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import WilliamsR

class master_mind_triple_wpr_strategy(Strategy):
    def __init__(self):
        super(master_mind_triple_wpr_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetDisplay("Trade Volume", "Target net position volume", "Trading")
        self._oversold_level = self.Param("OversoldLevel", -99.99) \
            .SetDisplay("Oversold Level", "All Williams %R must be below this level", "Signals")
        self._overbought_level = self.Param("OverboughtLevel", -0.01) \
            .SetDisplay("Overbought Level", "All Williams %R must be above this level", "Signals")
        self._stop_loss_steps = self.Param("StopLossSteps", 2000) \
            .SetDisplay("Stop Loss (steps)", "Protective stop distance in price steps", "Risk")
        self._take_profit_steps = self.Param("TakeProfitSteps", 0) \
            .SetDisplay("Take Profit (steps)", "Take profit distance in price steps", "Risk")
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 0) \
            .SetDisplay("Trailing Stop (steps)", "Trailing stop distance in price steps", "Risk")
        self._trailing_step_steps = self.Param("TrailingStepSteps", 1) \
            .SetDisplay("Trailing Step (steps)", "Minimal improvement before trailing adjusts", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe to process", "General")

        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @property
    def StopLossSteps(self):
        return self._stop_loss_steps.Value

    @property
    def TakeProfitSteps(self):
        return self._take_profit_steps.Value

    @property
    def TrailingStopSteps(self):
        return self._trailing_stop_steps.Value

    @property
    def TrailingStepSteps(self):
        return self._trailing_step_steps.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(master_mind_triple_wpr_strategy, self).OnStarted(time)

        self.Volume = float(self.TradeVolume)

        self._wpr26 = WilliamsR()
        self._wpr26.Length = 26
        self._wpr27 = WilliamsR()
        self._wpr27.Length = 27
        self._wpr29 = WilliamsR()
        self._wpr29.Length = 29
        self._wpr30 = WilliamsR()
        self._wpr30.Length = 30

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._wpr26, self._wpr27, self._wpr29, self._wpr30, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, wpr26_value, wpr27_value, wpr29_value, wpr30_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._wpr26.IsFormed or not self._wpr27.IsFormed or not self._wpr29.IsFormed or not self._wpr30.IsFormed:
            return

        wpr26_value = float(wpr26_value)
        wpr27_value = float(wpr27_value)
        wpr29_value = float(wpr29_value)
        wpr30_value = float(wpr30_value)

        self._update_trailing_stops(candle)
        self._try_close_by_risk(candle)

        oversold = float(self.OversoldLevel)
        overbought = float(self.OverboughtLevel)

        is_oversold = (wpr26_value <= oversold and wpr27_value <= oversold and
                       wpr29_value <= oversold and wpr30_value <= oversold)
        is_overbought = (wpr26_value >= overbought and wpr27_value >= overbought and
                         wpr29_value >= overbought and wpr30_value >= overbought)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if is_oversold:
            self._open_long(candle)
        elif is_overbought:
            self._open_short(candle)

    def _open_long(self, candle):
        target = float(self.TradeVolume)
        if target <= 0:
            return
        current = self.Position
        difference = target - current
        if difference <= 0:
            return

        existing_long = max(current, 0)
        self.BuyMarket(difference)

        entry_price = float(candle.ClosePrice)
        self._update_long_state(existing_long, difference, entry_price)

    def _open_short(self, candle):
        target = -float(self.TradeVolume)
        if target >= 0:
            return
        current = self.Position
        difference = current - target
        if difference <= 0:
            return

        existing_short = max(-current, 0)
        self.SellMarket(difference)

        entry_price = float(candle.ClosePrice)
        self._update_short_state(existing_short, difference, entry_price)

    def _try_close_by_risk(self, candle):
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop_price is not None and low_price <= self._long_stop_price:
                self.SellMarket(self.Position)
                self._reset_long_state()
                return
            if self._long_take_price is not None and high_price >= self._long_take_price:
                self.SellMarket(self.Position)
                self._reset_long_state()

        elif self.Position < 0:
            short_volume = abs(self.Position)
            if self._short_stop_price is not None and high_price >= self._short_stop_price:
                self.BuyMarket(short_volume)
                self._reset_short_state()
                return
            if self._short_take_price is not None and low_price <= self._short_take_price:
                self.BuyMarket(short_volume)
                self._reset_short_state()

    def _update_trailing_stops(self, candle):
        trail_steps = self.TrailingStopSteps
        trail_step_steps = self.TrailingStepSteps
        if trail_steps <= 0 or trail_step_steps <= 0:
            return

        step = self._get_step_size()
        trailing_distance = trail_steps * step
        trailing_step = trail_step_steps * step
        close_price = float(candle.ClosePrice)

        if self.Position > 0 and self._long_entry_price is not None:
            profit = close_price - self._long_entry_price
            if profit > trailing_distance + trailing_step:
                new_stop = close_price - trailing_distance
                if self._long_stop_price is None or new_stop > self._long_stop_price + trailing_step:
                    self._long_stop_price = new_stop

        elif self.Position < 0 and self._short_entry_price is not None:
            profit = self._short_entry_price - close_price
            if profit > trailing_distance + trailing_step:
                new_stop = close_price + trailing_distance
                if self._short_stop_price is None or new_stop < self._short_stop_price - trailing_step:
                    self._short_stop_price = new_stop

    def _update_long_state(self, existing_volume, added_volume, entry_price):
        total = existing_volume + added_volume
        if total <= 0:
            self._reset_long_state()
            return

        if self._long_entry_price is None or existing_volume <= 0:
            self._long_entry_price = entry_price
        else:
            self._long_entry_price = (self._long_entry_price * existing_volume + entry_price * added_volume) / total

        step = self._get_step_size()

        sl_steps = self.StopLossSteps
        if sl_steps > 0:
            self._long_stop_price = self._long_entry_price - sl_steps * step
        elif self.TrailingStopSteps <= 0:
            self._long_stop_price = None

        tp_steps = self.TakeProfitSteps
        self._long_take_price = self._long_entry_price + tp_steps * step if tp_steps > 0 else None

        self._reset_short_state()

    def _update_short_state(self, existing_volume, added_volume, entry_price):
        total = existing_volume + added_volume
        if total <= 0:
            self._reset_short_state()
            return

        if self._short_entry_price is None or existing_volume <= 0:
            self._short_entry_price = entry_price
        else:
            self._short_entry_price = (self._short_entry_price * existing_volume + entry_price * added_volume) / total

        step = self._get_step_size()

        sl_steps = self.StopLossSteps
        if sl_steps > 0:
            self._short_stop_price = self._short_entry_price + sl_steps * step
        elif self.TrailingStopSteps <= 0:
            self._short_stop_price = None

        tp_steps = self.TakeProfitSteps
        self._short_take_price = self._short_entry_price - tp_steps * step if tp_steps > 0 else None

        self._reset_long_state()

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_price = None

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_price = None

    def _get_step_size(self):
        if self.Security is not None:
            ps = self.Security.PriceStep
            if ps is not None and float(ps) > 0:
                return float(ps)
        return 1.0

    def OnReseted(self):
        super(master_mind_triple_wpr_strategy, self).OnReseted()
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_price = None
        self._short_take_price = None

    def CreateClone(self):
        return master_mind_triple_wpr_strategy()
