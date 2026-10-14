import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, DateTime, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class boilerplate_configurable_strategy(Strategy):
    """
    Boilerplate Configurable strategy.
    In SMA cross mode a cross of SMA(FastLength) over SMA(Length) gives the signal; in squeeze mode a close beyond the
    WideMultiplier Bollinger band that stays inside the NarrowMultiplier band does. Signals can be inverted and limited to one
    side, and an opposite signal reverses the position (or only closes it when that side is disabled). Entries are allowed only
    on selected days, inside the session and the date range, outside the daily exit period and the news window; the exit period
    and the news window also close open positions. Each entry freezes a stop of AtrMultiplier ATRs or MaxLossPerc of the price
    and a take profit StaticRr times that distance. Trading stops once the equity drawdown reaches MaxDrawdown.
    """

    def __init__(self):
        super(boilerplate_configurable_strategy, self).__init__()
        self._use_squeeze = self.Param("UseSqueeze", False).SetDisplay("Squeeze Mode", "Use the Bollinger squeeze mode instead of the SMA cross", "Mode")
        self._fast_length = self.Param("FastLength", 10).SetGreaterThanZero().SetDisplay("Fast Length", "Fast SMA period of the cross mode", "Indicators")
        self._length = self.Param("Length", 20).SetGreaterThanZero().SetDisplay("Length", "Slow SMA and Bollinger period", "Indicators")
        self._wide_multiplier = self.Param("WideMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Wide Multiplier", "Deviation multiplier of the band price has to break", "Indicators")
        self._narrow_multiplier = self.Param("NarrowMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Narrow Multiplier", "Deviation multiplier of the band price has to stay inside", "Indicators")
        self._allow_long = self.Param("AllowLong", True).SetDisplay("Allow Long", "Allow long entries", "Direction")
        self._allow_short = self.Param("AllowShort", True).SetDisplay("Allow Short", "Allow short entries", "Direction")
        self._invert = self.Param("Invert", False).SetDisplay("Invert", "Swap long and short signals", "Direction")
        self._use_atr_stops = self.Param("UseAtrStops", True).SetDisplay("ATR Stops", "Size the stop with ATR instead of a fixed percent", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiple of the stop distance", "Risk")
        self._max_loss_perc = self.Param("MaxLossPerc", 0.02).SetGreaterThanZero().SetDisplay("Max Loss", "Stop distance as a fraction of the entry price", "Risk")
        self._static_rr = self.Param("StaticRr", 2.0).SetGreaterThanZero().SetDisplay("Risk/Reward", "Take profit distance as a multiple of the stop distance", "Risk")
        self._max_drawdown = self.Param("MaxDrawdown", 0.1).SetGreaterThanZero().SetDisplay("Max Drawdown", "Equity drawdown fraction that stops trading", "Risk")
        self._trade_weekends = self.Param("TradeWeekends", True).SetDisplay("Trade Weekends", "Allow entries on Saturday and Sunday", "Filters")
        self._session_start_hour = self.Param("SessionStartHour", 0).SetRange(0, 23).SetDisplay("Session Start", "Session start hour (UTC)", "Filters")
        self._session_end_hour = self.Param("SessionEndHour", 24).SetRange(1, 24).SetDisplay("Session End", "Session end hour (UTC, exclusive)", "Filters")
        self._start_date = self.Param("StartDate", DateTime(2000, 1, 1)).SetDisplay("Start Date", "First date entries are allowed", "Filters")
        self._end_date = self.Param("EndDate", DateTime(2100, 1, 1)).SetDisplay("End Date", "Last date entries are allowed", "Filters")
        self._use_exit_period = self.Param("UseExitPeriod", False).SetDisplay("Use Exit Period", "Close positions and block entries in the daily exit period", "Filters")
        self._exit_start_hour = self.Param("ExitStartHour", 22).SetRange(0, 23).SetDisplay("Exit Start", "Exit period start hour (UTC)", "Filters")
        self._exit_end_hour = self.Param("ExitEndHour", 24).SetRange(1, 24).SetDisplay("Exit End", "Exit period end hour (UTC, exclusive)", "Filters")
        self._use_news_filter = self.Param("UseNewsFilter", False).SetDisplay("Use News Filter", "Close positions and block entries around the daily news time", "News")
        self._news_hour = self.Param("NewsHour", 12).SetRange(0, 23).SetDisplay("News Hour", "News hour (UTC)", "News")
        self._news_minute = self.Param("NewsMinute", 30).SetRange(0, 59).SetDisplay("News Minute", "News minute", "News")
        self._news_window = self.Param("NewsWindow", 5).SetNotNegative().SetDisplay("News Window", "Minutes before and after the news time", "News")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = None
        self._take_price = None
        self._initial_equity = Decimal(0)
        self._peak_equity = Decimal(0)
        self._halted = False

    def OnReseted(self):
        super(boilerplate_configurable_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(boilerplate_configurable_strategy, self).OnStarted2(time)

        self._reset_state()
        portfolio = self.Portfolio
        if portfolio is not None:
            begin = portfolio.BeginValue
            if begin is not None and begin > 0:
                self._initial_equity = begin
            elif portfolio.CurrentValue is not None:
                self._initial_equity = portfolio.CurrentValue
        self._peak_equity = self._initial_equity

        fast = SimpleMovingAverage()
        fast.Length = self._fast_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._length.Value
        deviation = StandardDeviation()
        deviation.Length = self._length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, slow, deviation, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, deviation_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed or not deviation_value.IsFormed or not atr_value.IsFormed:
            return

        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        # Stop and take profit frozen at entry.
        if self.Position > 0 and self._stop_price is not None and self._take_price is not None and (candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price):
            self._close_position()
            return

        if self.Position < 0 and self._stop_price is not None and self._take_price is not None and (candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price):
            self._close_position()
            return

        if self._initial_equity > 0:
            equity = self._initial_equity + self.PnL
            if equity > self._peak_equity:
                self._peak_equity = equity
            if self._peak_equity > 0 and (self._peak_equity - equity) / self._peak_equity >= Decimal(self._max_drawdown.Value):
                self._halted = True

        time = candle.OpenTime

        if self._halted or self._in_exit_period(time) or self._in_news_window(time):
            self._close_position()
            return

        if self._use_squeeze.Value:
            sigma = deviation_value.GetValue[Decimal](None)
            wide = Decimal(self._wide_multiplier.Value) * sigma
            narrow = Decimal(self._narrow_multiplier.Value) * sigma
            long_signal = close > slow + wide and close < slow + narrow
            short_signal = close < slow - wide and close > slow - narrow
        else:
            long_signal = prev_fast is not None and prev_slow is not None and prev_fast <= prev_slow and fast > slow
            short_signal = prev_fast is not None and prev_slow is not None and prev_fast >= prev_slow and fast < slow

        if self._invert.Value:
            long_signal, short_signal = short_signal, long_signal

        can_enter = self._in_session(time)
        if self._use_atr_stops.Value:
            stop_distance = Decimal(self._atr_multiplier.Value) * atr_value.GetValue[Decimal](None)
        else:
            stop_distance = Decimal(self._max_loss_perc.Value) * close
        rr = Decimal(self._static_rr.Value)

        if long_signal and self.Position <= 0:
            if self._allow_long.Value and can_enter and stop_distance > 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._stop_price = close - stop_distance
                self._take_price = close + rr * stop_distance
            elif self.Position < 0:
                self._close_position()
        elif short_signal and self.Position >= 0:
            if self._allow_short.Value and can_enter and stop_distance > 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._stop_price = close + stop_distance
                self._take_price = close - rr * stop_distance
            elif self.Position > 0:
                self._close_position()

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)
        self._stop_price = None
        self._take_price = None

    def _in_session(self, time):
        if not self._trade_weekends.Value and (time.DayOfWeek == DayOfWeek.Saturday or time.DayOfWeek == DayOfWeek.Sunday):
            return False
        if time.Date < self._start_date.Value.Date or time.Date > self._end_date.Value.Date:
            return False
        return time.Hour >= self._session_start_hour.Value and time.Hour < self._session_end_hour.Value

    def _in_exit_period(self, time):
        return self._use_exit_period.Value and time.Hour >= self._exit_start_hour.Value and time.Hour < self._exit_end_hour.Value

    def _in_news_window(self, time):
        if not self._use_news_filter.Value:
            return False
        news = time.Date.AddHours(self._news_hour.Value).AddMinutes(self._news_minute.Value)
        return abs((time - news).TotalMinutes) <= self._news_window.Value

    def CreateClone(self):
        return boilerplate_configurable_strategy()
