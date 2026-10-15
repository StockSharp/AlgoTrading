import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class stopreversal_tm_strategy(Strategy):
    """Stopreversal TM: trailing stop reversal strategy with configurable trading session filter."""

    def __init__(self):
        super(stopreversal_tm_strategy, self).__init__()

        self._allow_buy_entry = self.Param("AllowBuyEntry", True) \
            .SetDisplay("Allow Buy Entries", "Enable opening long positions on bullish signals", "Signals")
        self._allow_sell_entry = self.Param("AllowSellEntry", True) \
            .SetDisplay("Allow Sell Entries", "Enable opening short positions on bearish signals", "Signals")
        self._allow_buy_exit = self.Param("AllowBuyExit", True) \
            .SetDisplay("Allow Long Exits", "Close existing long positions when a sell signal arrives", "Signals")
        self._allow_sell_exit = self.Param("AllowSellExit", True) \
            .SetDisplay("Allow Short Exits", "Close existing short positions when a buy signal arrives", "Signals")
        self._use_time_filter = self.Param("UseTimeFilter", False) \
            .SetDisplay("Use Time Filter", "Restrict trading to the configured session", "Session")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Session start hour (0-23)", "Session")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Session start minute (0-59)", "Session")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Session end hour (0-23)", "Session")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Session end minute (0-59)", "Session")
        self._n_pips = self.Param("Npips", 0.004) \
            .SetGreaterThanZero() \
            .SetDisplay("Sensitivity", "Relative offset used to build the trailing stop", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar Delay", "Number of completed bars to wait before acting", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used for calculations", "General")
        # Price types: 1=Close, 2=Open, 3=High, 4=Low, 5=Median, 6=Typical, 7=Weighted,
        #              8=Simple, 9=Quarter, 10=TrendFollow0, 11=TrendFollow1, 12=Demark
        self._applied_price = self.Param("AppliedPrice", 1) \
            .SetDisplay("Applied Price", "Price source for the trailing stop", "Indicator")

        self._signal_queue = []
        self._previous_applied_price = None
        self._previous_stop_level = None

    @property
    def AllowBuyEntry(self):
        return self._allow_buy_entry.Value
    @property
    def AllowSellEntry(self):
        return self._allow_sell_entry.Value
    @property
    def AllowBuyExit(self):
        return self._allow_buy_exit.Value
    @property
    def AllowSellExit(self):
        return self._allow_sell_exit.Value
    @property
    def UseTimeFilter(self):
        return self._use_time_filter.Value
    @property
    def StartHour(self):
        return int(self._start_hour.Value)
    @property
    def StartMinute(self):
        return int(self._start_minute.Value)
    @property
    def EndHour(self):
        return int(self._end_hour.Value)
    @property
    def EndMinute(self):
        return int(self._end_minute.Value)
    @property
    def Npips(self):
        return float(self._n_pips.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def AppliedPrice(self):
        return int(self._applied_price.Value)

    def _get_applied_price(self, candle):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        ap = self.AppliedPrice

        if ap == 1:
            return c
        elif ap == 2:
            return o
        elif ap == 3:
            return h
        elif ap == 4:
            return lo
        elif ap == 5:
            return (h + lo) / 2.0
        elif ap == 6:
            return (c + h + lo) / 3.0
        elif ap == 7:
            return (2.0 * c + h + lo) / 4.0
        elif ap == 8:
            return (o + c) / 2.0
        elif ap == 9:
            return (o + c + h + lo) / 4.0
        elif ap == 10:
            if c > o:
                return h
            elif c < o:
                return lo
            return c
        elif ap == 11:
            if c > o:
                return (h + c) / 2.0
            elif c < o:
                return (lo + c) / 2.0
            return c
        elif ap == 12:
            return self._calc_demark(candle)
        return c

    def _calc_demark(self, candle):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        res = h + lo + c
        if c < o:
            res = (res + lo) / 2.0
        elif c > o:
            res = (res + h) / 2.0
        else:
            res = (res + c) / 2.0
        return ((res - lo) + (res - h)) / 2.0

    def _calc_trailing_stop(self, price, prev_price, prev_stop):
        shift = self.Npips
        if price == prev_stop:
            return prev_stop
        if prev_price < prev_stop and price < prev_stop:
            return min(prev_stop, price * (1.0 + shift))
        if prev_price > prev_stop and price > prev_stop:
            return max(prev_stop, price * (1.0 - shift))
        if price > prev_stop:
            return price * (1.0 - shift)
        return price * (1.0 + shift)

    def _is_within_trading_window(self, time):
        sh = self.StartHour
        sm = self.StartMinute
        eh = self.EndHour
        em = self.EndMinute

        start_minutes = sh * 60 + sm
        end_minutes = eh * 60 + em
        current_minutes = time.Hour * 60 + time.Minute

        if start_minutes == end_minutes:
            return False
        if start_minutes < end_minutes:
            return current_minutes >= start_minutes and current_minutes < end_minutes
        return current_minutes >= start_minutes or current_minutes < end_minutes

    def OnStarted2(self, time):
        super(stopreversal_tm_strategy, self).OnStarted2(time)

        self._previous_applied_price = None
        self._previous_stop_level = None
        self._signal_queue = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._get_applied_price(candle)

        if self._previous_applied_price is None or self._previous_stop_level is None:
            self._previous_applied_price = price
            self._previous_stop_level = price
            self._enqueue_signal((False, False, False, False), candle.CloseTime)
            return

        prev_price = self._previous_applied_price
        prev_stop = self._previous_stop_level

        trailing_stop = self._calc_trailing_stop(price, prev_price, prev_stop)

        buy_signal = price > trailing_stop and prev_price < prev_stop
        sell_signal = price < trailing_stop and prev_price > prev_stop

        self._previous_stop_level = trailing_stop
        self._previous_applied_price = price

        action = (
            buy_signal and self.AllowBuyEntry,
            sell_signal and self.AllowSellEntry,
            sell_signal and self.AllowBuyExit,
            buy_signal and self.AllowSellExit
        )

        self._enqueue_signal(action, candle.CloseTime)

    def _enqueue_signal(self, signal, current_time):
        self._signal_queue.append(signal)

        while len(self._signal_queue) > self.SignalBar:
            action = self._signal_queue.pop(0)
            self._handle_signal(action, current_time)

    def _handle_signal(self, signal, current_time):
        open_long, open_short, close_long, close_short = signal
        in_window = not self.UseTimeFilter or self._is_within_trading_window(current_time)

        if self.UseTimeFilter and not in_window and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()

        if close_long and self.Position > 0:
            self.SellMarket()

        if close_short and self.Position < 0:
            self.BuyMarket()

        if not self.UseTimeFilter or in_window:
            if open_long and self.Position <= 0:
                self.BuyMarket()
            if open_short and self.Position >= 0:
                self.SellMarket()

    def OnReseted(self):
        super(stopreversal_tm_strategy, self).OnReseted()
        self._previous_applied_price = None
        self._previous_stop_level = None
        self._signal_queue = []

    def CreateClone(self):
        return stopreversal_tm_strategy()
