import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class percentage_crossover_strategy(Strategy):
    """Percentage Crossover indicator-based strategy with time filter."""

    def __init__(self):
        super(percentage_crossover_strategy, self).__init__()

        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable Buy Entries", "Allow opening long positions", "General")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable Sell Entries", "Allow opening short positions", "General")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Enable Buy Exits", "Allow closing long positions", "General")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Enable Sell Exits", "Allow closing short positions", "General")
        self._use_time_filter = self.Param("UseTimeFilter", True) \
            .SetDisplay("Use Time Filter", "Restrict trading to specific hours", "Time Filter")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Trading window start hour", "Time Filter")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Trading window start minute", "Time Filter")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Trading window end hour", "Time Filter")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Trading window end minute", "Time Filter")
        self._percent = self.Param("Percent", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Percent", "Percentage offset for the indicator", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Bar", "Closed bars to look back for the signal", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for signal candles", "Data")

        self._color_history = []
        self._prev_middle = None
        self._last_color = None

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
    @property
    def UseTimeFilter(self):
        return self._use_time_filter.Value
    @property
    def StartHour(self):
        return self._start_hour.Value
    @property
    def StartMinute(self):
        return self._start_minute.Value
    @property
    def EndHour(self):
        return self._end_hour.Value
    @property
    def EndMinute(self):
        return self._end_minute.Value
    @property
    def Percent(self):
        return self._percent.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(percentage_crossover_strategy, self).OnStarted2(time)
        self._color_history = []
        self._prev_middle = None
        self._last_color = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        pf = float(self.Percent) / 100.0

        if self._prev_middle is None:
            self._prev_middle = close
            self._last_color = 0
            self._color_history = [0]
            return

        prev_mid = self._prev_middle
        lower_b = close * (1.0 - pf)
        upper_b = close * (1.0 + pf)

        middle = prev_mid
        if lower_b > prev_mid:
            middle = lower_b
        elif upper_b < prev_mid:
            middle = upper_b

        color = self._last_color if self._last_color is not None else 0
        if middle > prev_mid:
            color = 0
        elif middle < prev_mid:
            color = 1

        self._prev_middle = middle
        self._last_color = color

        self._color_history.append(color)
        max_size = max(self.SignalBar + 2, 4)
        while len(self._color_history) > max_size:
            self._color_history.pop(0)

        ci = len(self._color_history) - self.SignalBar
        if ci <= 0:
            return
        pi = ci - 1
        if pi < 0:
            return

        cc = self._color_history[ci]
        pc = self._color_history[pi]

        buy_open = self.BuyPosOpen and cc == 0 and pc == 1
        sell_open = self.SellPosOpen and cc == 1 and pc == 0
        buy_close = self.BuyPosClose and cc == 1
        sell_close = self.SellPosClose and cc == 0

        in_window = (not self.UseTimeFilter) or self._is_trading_time(candle.CloseTime)

        if self.UseTimeFilter and not in_window:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return

        if buy_close and self.Position > 0:
            self.SellMarket()
        if sell_close and self.Position < 0:
            self.BuyMarket()

        if not in_window:
            return

        if buy_open and self.Position <= 0:
            self.BuyMarket()
        elif sell_open and self.Position >= 0:
            self.SellMarket()

    def _is_trading_time(self, time):
        h = time.Hour
        m = time.Minute
        sh = self.StartHour
        sm = self.StartMinute
        eh = self.EndHour
        em = self.EndMinute

        if sh < eh:
            if h == sh and m >= sm:
                return True
            if h > sh and h < eh:
                return True
            if h > sh and h == eh and m < em:
                return True
            return False
        if sh == eh:
            return h == sh and m >= sm and m < em
        if h >= sh and m >= sm:
            return True
        if h < eh:
            return True
        if h == eh and m < em:
            return True
        return False

    def OnReseted(self):
        super(percentage_crossover_strategy, self).OnReseted()
        self._color_history = []
        self._prev_middle = None
        self._last_color = None

    def CreateClone(self):
        return percentage_crossover_strategy()
