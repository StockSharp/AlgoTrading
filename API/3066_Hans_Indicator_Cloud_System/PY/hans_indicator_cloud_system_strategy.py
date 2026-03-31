import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class hans_indicator_cloud_system_strategy(Strategy):
    _PERIOD1_START = 4
    _PERIOD1_END = 8
    _PERIOD2_END = 12

    def __init__(self):
        super(hans_indicator_cloud_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle type", "Primary timeframe analysed by the strategy", "General")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal bar", "Historical bar index inspected for colour changes", "Signals")
        self._local_time_zone = self.Param("LocalTimeZone", 0) \
            .SetDisplay("Local timezone", "Broker/server timezone (hours)", "Time zones")
        self._destination_time_zone = self.Param("DestinationTimeZone", 4) \
            .SetDisplay("Destination timezone", "Target timezone for Hans ranges (hours)", "Time zones")
        self._pips_for_entry = self.Param("PipsForEntry", 300.0) \
            .SetDisplay("Breakout buffer", "Extra price steps added above/below the session ranges", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 48) \
            .SetDisplay("Cooldown bars", "Bars to wait after a close or entry", "Trading")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable long entries", "Allow opening new long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable short entries", "Allow opening new short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Enable long exits", "Allow closing existing longs", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Enable short exits", "Allow closing existing shorts", "Trading")

        self._color_history = []
        self._current_day_date = None
        self._p1_high = None
        self._p1_low = None
        self._p1_seen = False
        self._p1_closed = False
        self._p2_high = None
        self._p2_low = None
        self._p2_seen = False
        self._p2_closed = False
        self._time_shift_hours = 0
        self._cooldown_left = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def LocalTimeZone(self):
        return self._local_time_zone.Value
    @property
    def DestinationTimeZone(self):
        return self._destination_time_zone.Value
    @property
    def PipsForEntry(self):
        return self._pips_for_entry.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value
    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value
    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value
    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    def OnReseted(self):
        super(hans_indicator_cloud_system_strategy, self).OnReseted()
        self._color_history = []
        self._current_day_date = None
        self._p1_high = None
        self._p1_low = None
        self._p1_seen = False
        self._p1_closed = False
        self._p2_high = None
        self._p2_low = None
        self._p2_seen = False
        self._p2_closed = False
        self._cooldown_left = 0

    def OnStarted2(self, time):
        super(hans_indicator_cloud_system_strategy, self).OnStarted2(time)
        self._time_shift_hours = self.DestinationTimeZone - self.LocalTimeZone
        self._current_day_date = None
        self._color_history = []
        self._cooldown_left = 0
        self._p1_high = None
        self._p1_low = None
        self._p1_seen = False
        self._p1_closed = False
        self._p2_high = None
        self._p2_low = None
        self._p2_seen = False
        self._p2_closed = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        color = self._calculate_color(candle)
        self._color_history.append(color)

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        max_history = max(5, self.SignalBar + 3)
        if len(self._color_history) > max_history:
            self._color_history.pop(0)

        target_index = len(self._color_history) - 1 - self.SignalBar
        if target_index <= 0:
            return

        col0 = self._color_history[target_index]
        col1 = self._color_history[target_index - 1]

        bullish_breakout = col1 == 0 or col1 == 1
        bearish_breakout = col1 == 3 or col1 == 4

        should_close_short = self.SellPosClose and bullish_breakout
        should_open_long = self.BuyPosOpen and bullish_breakout and col0 != 0 and col0 != 1
        should_close_long = self.BuyPosClose and bearish_breakout
        should_open_short = self.SellPosOpen and bearish_breakout and col0 != 3 and col0 != 4

        if should_close_long and self.Position > 0:
            self.SellMarket()
            self._cooldown_left = self.CooldownBars
            return

        if should_close_short and self.Position < 0:
            self.BuyMarket()
            self._cooldown_left = self.CooldownBars
            return

        if self._cooldown_left == 0 and should_open_long and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_left = self.CooldownBars
        elif self._cooldown_left == 0 and should_open_short and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_left = self.CooldownBars

    def _calculate_color(self, candle):
        shifted_hour = candle.OpenTime.Hour + self._time_shift_hours
        shifted_date = candle.OpenTime.Date
        if shifted_hour >= 24:
            shifted_hour -= 24
            shifted_date = shifted_date.AddDays(1)
        elif shifted_hour < 0:
            shifted_hour += 24
            shifted_date = shifted_date.AddDays(-1)

        if self._current_day_date is None or self._current_day_date != shifted_date:
            self._current_day_date = shifted_date
            self._p1_high = None
            self._p1_low = None
            self._p1_seen = False
            self._p1_closed = False
            self._p2_high = None
            self._p2_low = None
            self._p2_seen = False
            self._p2_closed = False

        self._update_session_extremes(candle, shifted_hour)

        zone = self._get_active_zone()
        if zone is None:
            return 2

        upper, lower = zone
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        if close > upper:
            return 0 if close >= open_p else 1
        if close < lower:
            return 4 if close <= open_p else 3
        return 2

    def _update_session_extremes(self, candle, local_hour):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if local_hour >= self._PERIOD1_START and local_hour < self._PERIOD1_END:
            self._p1_seen = True
            self._p1_high = max(self._p1_high, high) if self._p1_high is not None else high
            self._p1_low = min(self._p1_low, low) if self._p1_low is not None else low
        elif local_hour >= self._PERIOD1_END and local_hour < self._PERIOD2_END:
            if not self._p1_closed and self._p1_seen:
                self._p1_closed = True
            self._p2_seen = True
            self._p2_high = max(self._p2_high, high) if self._p2_high is not None else high
            self._p2_low = min(self._p2_low, low) if self._p2_low is not None else low
        else:
            if not self._p1_closed and self._p1_seen and local_hour >= self._PERIOD1_END:
                self._p1_closed = True
            if not self._p2_closed and self._p2_seen and local_hour >= self._PERIOD2_END:
                self._p2_closed = True

        if local_hour >= self._PERIOD2_END and self._p2_seen:
            self._p2_closed = True

    def _get_active_zone(self):
        entry_offset = self._get_entry_offset()
        if self._p2_closed and self._p2_high is not None and self._p2_low is not None:
            return (self._p2_high + entry_offset, self._p2_low - entry_offset)
        if self._p1_closed and self._p1_high is not None and self._p1_low is not None:
            return (self._p1_high + entry_offset, self._p1_low - entry_offset)
        return None

    def _get_entry_offset(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0
        return float(self.PipsForEntry) * step

    def CreateClone(self):
        return hans_indicator_cloud_system_strategy()
