import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy


class time_zone_pivots_open_system_strategy(Strategy):
    def __init__(self):
        super(time_zone_pivots_open_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Timeframe that feeds the Time Zone Pivots logic.", "General")
        self._order_volume = self.Param("OrderVolume", Decimal(0.1)) \
            .SetNotNegative() \
            .SetDisplay("Order volume", "Volume used when opening a new position.", "Trading")
        self._start_hour = self.Param("StartHour", 0) \
            .SetNotNegative() \
            .SetDisplay("Start hour", "Hour (0-23) whose opening price anchors the bands.", "Indicator")
        self._offset_points = self.Param("OffsetPoints", Decimal(250)) \
            .SetNotNegative() \
            .SetDisplay("Offset (points)", "Distance from the anchor price expressed in price steps.", "Indicator")
        self._signal_bar = self.Param("SignalBar", 2) \
            .SetNotNegative() \
            .SetDisplay("Signal bar", "Shift of the confirmation candle used to trigger trades.", "Signals")
        self._stop_loss_points = self.Param("StopLossPoints", Decimal(1000)) \
            .SetNotNegative() \
            .SetDisplay("Stop loss (points)", "Protective stop distance in price steps.", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", Decimal(2000)) \
            .SetNotNegative() \
            .SetDisplay("Take profit (points)", "Profit target distance in price steps.", "Risk")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable long entries", "Allow opening long positions after bullish breakouts.", "Signals")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable short entries", "Allow opening short positions after bearish breakouts.", "Signals")
        self._close_long_on_bearish_break = self.Param("CloseLongOnBearishBreak", True) \
            .SetDisplay("Close longs on bearish break", "Exit long trades when price falls below lower band.", "Risk")
        self._close_short_on_bullish_break = self.Param("CloseShortOnBullishBreak", True) \
            .SetDisplay("Close shorts on bullish break", "Exit short trades when price rallies above upper band.", "Risk")

        self._price_step = Decimal(0)
        self._offset_distance = Decimal(0)
        self._anchor_price = None
        self._anchor_date = None
        self._upper_zone = Decimal(0)
        self._lower_zone = Decimal(0)
        self._candle_span = TimeSpan.Zero
        self._next_long_trade_time = None
        self._next_short_trade_time = None
        self._signal_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def OrderVolume(self):
        return self._order_volume.Value
    @property
    def StartHour(self):
        h = self._start_hour.Value
        if h < 0:
            return 0
        if h > 23:
            return 23
        return h
    @property
    def OffsetPoints(self):
        return self._offset_points.Value
    @property
    def SignalBar(self):
        return max(1, self._signal_bar.Value)
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value
    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value
    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value
    @property
    def CloseLongOnBearishBreak(self):
        return self._close_long_on_bearish_break.Value
    @property
    def CloseShortOnBullishBreak(self):
        return self._close_short_on_bullish_break.Value

    def OnReseted(self):
        super(time_zone_pivots_open_system_strategy, self).OnReseted()
        self._anchor_price = None
        self._anchor_date = None
        self._upper_zone = Decimal(0)
        self._lower_zone = Decimal(0)
        self._signal_history = []
        self._next_long_trade_time = None
        self._next_short_trade_time = None

    def OnStarted2(self, time):
        super(time_zone_pivots_open_system_strategy, self).OnStarted2(time)
        self.Volume = self.OrderVolume

        sec = self.Security
        self._price_step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)
        if self._price_step <= Decimal(0):
            self._price_step = Decimal(1)

        arg = self.CandleType.Arg
        if isinstance(arg, TimeSpan) and arg > TimeSpan.Zero:
            self._candle_span = arg
        else:
            self._candle_span = TimeSpan.FromHours(1)

        self._offset_distance = self.OffsetPoints * self._price_step

        stop_loss_distance = self.StopLossPoints * self._price_step
        take_profit_distance = self.TakeProfitPoints * self._price_step

        if stop_loss_distance > Decimal(0) or take_profit_distance > Decimal(0):
            sl_unit = Unit(stop_loss_distance, UnitTypes.Absolute) if stop_loss_distance > Decimal(0) else Unit()
            tp_unit = Unit(take_profit_distance, UnitTypes.Absolute) if take_profit_distance > Decimal(0) else Unit()
            self.StartProtection(tp_unit, sl_unit, False, None, None, True, False)

        self._anchor_price = None
        self._anchor_date = None
        self._upper_zone = Decimal(0)
        self._lower_zone = Decimal(0)
        self._signal_history = []
        self._next_long_trade_time = None
        self._next_short_trade_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._offset_distance = self.OffsetPoints * self._price_step
        self.Volume = self.OrderVolume

        self._update_anchor(candle)
        signal = self._calculate_signal(candle)
        self._record_signal(candle.OpenTime, signal)

        if len(self._signal_history) <= self.SignalBar:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        confirm_index = self.SignalBar
        current_index = self.SignalBar - 1

        if current_index < 0 or confirm_index >= len(self._signal_history):
            return

        current_signal = self._signal_history[current_index][0]
        confirm_signal = self._signal_history[confirm_index][0]
        confirm_time = self._signal_history[confirm_index][1]

        bullish_breakout = confirm_signal <= 1
        bearish_breakout = confirm_signal >= 3

        position = self.Position

        if position > Decimal(0) and bearish_breakout and self.CloseLongOnBearishBreak:
            self.SellMarket(position)
            position = self.Position

        if position < Decimal(0) and bullish_breakout and self.CloseShortOnBullishBreak:
            self.BuyMarket(Math.Abs(position))
            position = self.Position

        volume = self.OrderVolume
        if volume <= Decimal(0):
            return

        signal_time = confirm_time + self._candle_span
        candle_time = candle.CloseTime if candle.CloseTime != type(candle.CloseTime)() else candle.OpenTime

        if self.EnableLongEntry and bullish_breakout and current_signal > 1 and position == Decimal(0):
            if self._next_long_trade_time is None or candle_time >= self._next_long_trade_time:
                self.BuyMarket(volume)
                self._next_long_trade_time = signal_time

        if self.EnableShortEntry and bearish_breakout and current_signal < 3 and position == Decimal(0):
            if self._next_short_trade_time is None or candle_time >= self._next_short_trade_time:
                self.SellMarket(volume)
                self._next_short_trade_time = signal_time

    def _update_anchor(self, candle):
        candle_date = candle.OpenTime.Date
        hour = candle.OpenTime.Hour

        if hour == self.StartHour and (self._anchor_date is None or self._anchor_date != candle_date):
            self._anchor_date = candle_date
            self._anchor_price = candle.OpenPrice

        if self._anchor_price is not None:
            self._upper_zone = self._anchor_price + self._offset_distance
            self._lower_zone = self._anchor_price - self._offset_distance

    def _calculate_signal(self, candle):
        if self._anchor_price is None:
            return 2

        close = candle.ClosePrice
        open_p = candle.OpenPrice

        if close > self._upper_zone:
            return 0 if close >= open_p else 1
        if close < self._lower_zone:
            return 4 if close <= open_p else 3
        return 2

    def _record_signal(self, open_time, signal):
        self._signal_history.insert(0, (signal, open_time))
        max_cap = max(self.SignalBar + 2, 4)
        if len(self._signal_history) > max_cap:
            del self._signal_history[max_cap:]

    def CreateClone(self):
        return time_zone_pivots_open_system_strategy()
