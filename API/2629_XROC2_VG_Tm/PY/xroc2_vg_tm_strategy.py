import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage, DecimalIndicatorValue
)


class xroc2_vg_tm_strategy(Strategy):
    """XROC2 VG with time filter: dual smoothed ROC crossover strategy."""

    def __init__(self):
        super(xroc2_vg_tm_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._roc_period1 = self.Param("RocPeriod1", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast ROC Period", "Lookback for fast ROC", "Indicator")
        self._roc_period2 = self.Param("RocPeriod2", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow ROC Period", "Lookback for slow ROC", "Indicator")
        self._smooth_len1 = self.Param("SmoothLength1", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Smoothing", "Smoothing for fast line", "Indicator")
        self._smooth_len2 = self.Param("SmoothLength2", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Smoothing", "Smoothing for slow line", "Indicator")
        self._signal_shift = self.Param("SignalShift", 0) \
            .SetDisplay("Signal Shift", "Bars back to read signals", "Logic")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Long Entry", "Enable long positions", "Trading")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Short Entry", "Enable short positions", "Trading")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Allow Long Exit", "Enable closing longs", "Trading")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Allow Short Exit", "Enable closing shorts", "Trading")
        self._use_time_filter = self.Param("UseTimeFilter", False) \
            .SetDisplay("Use Time Filter", "Restrict trading to time window", "Timing")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Session start hour", "Timing")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Session start minute", "Timing")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Session end hour", "Timing")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Session end minute", "Timing")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Volume for new positions", "Trading")
        self._stop_loss = self.Param("StopLoss", 0.0) \
            .SetDisplay("Stop Loss", "Stop distance in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 0.0) \
            .SetDisplay("Take Profit", "Target distance in price units", "Risk")

        self._close_history = []
        self._fast_history = []
        self._slow_history = []
        self._smooth_fast = None
        self._smooth_slow = None
        self._long_entry = None
        self._short_entry = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RocPeriod1(self):
        return self._roc_period1.Value
    @property
    def RocPeriod2(self):
        return self._roc_period2.Value
    @property
    def SmoothLength1(self):
        return self._smooth_len1.Value
    @property
    def SmoothLength2(self):
        return self._smooth_len2.Value
    @property
    def SignalShift(self):
        return self._signal_shift.Value
    @property
    def AllowBuyOpen(self):
        return self._allow_buy_open.Value
    @property
    def AllowSellOpen(self):
        return self._allow_sell_open.Value
    @property
    def AllowBuyClose(self):
        return self._allow_buy_close.Value
    @property
    def AllowSellClose(self):
        return self._allow_sell_close.Value
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
    def OrderVolume(self):
        return self._order_volume.Value
    @property
    def StopLoss(self):
        return self._stop_loss.Value
    @property
    def TakeProfit(self):
        return self._take_profit.Value

    def OnStarted(self, time):
        super(xroc2_vg_tm_strategy, self).OnStarted(time)

        self.Volume = self.OrderVolume
        self._smooth_fast = ExponentialMovingAverage()
        self._smooth_fast.Length = self.SmoothLength1
        self._smooth_slow = ExponentialMovingAverage()
        self._smooth_slow.Length = self.SmoothLength2

        self._close_history = []
        self._fast_history = []
        self._slow_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        capacity = max(max(self.RocPeriod1, self.RocPeriod2) + self.SignalShift + 5, 8)
        self._close_history.insert(0, close)
        while len(self._close_history) > capacity:
            self._close_history.pop()

        fast_roc = self._calc_roc(self.RocPeriod1)
        slow_roc = self._calc_roc(self.RocPeriod2)

        if fast_roc is None or slow_roc is None:
            return

        fast_out = self._smooth_fast.Process(DecimalIndicatorValue(self._smooth_fast, fast_roc, candle.OpenTime))
        slow_out = self._smooth_slow.Process(DecimalIndicatorValue(self._smooth_slow, slow_roc, candle.OpenTime))

        fast_val = float(fast_out)
        slow_val = float(slow_out)

        hist_cap = self.SignalShift + 3
        self._fast_history.insert(0, fast_val)
        while len(self._fast_history) > hist_cap:
            self._fast_history.pop()
        self._slow_history.insert(0, slow_val)
        while len(self._slow_history) > hist_cap:
            self._slow_history.pop()

        ss = self.SignalShift
        if len(self._fast_history) <= ss + 1 or len(self._slow_history) <= ss + 1:
            return

        fc = self._fast_history[ss]
        fp = self._fast_history[ss + 1]
        sc = self._slow_history[ss]
        sp = self._slow_history[ss + 1]

        buy_open = self.AllowBuyOpen and fp <= sp and fc > sc
        sell_open = self.AllowSellOpen and fp >= sp and fc < sc
        buy_close = self.AllowBuyClose and fc < sc
        sell_close = self.AllowSellClose and fc > sc

        in_window = (not self.UseTimeFilter) or self._in_trade_window(candle.OpenTime)

        if self.UseTimeFilter and not in_window and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
            self._reset_state()
            return

        if self._try_risk(candle):
            return

        if sell_close and self.Position < 0:
            self.BuyMarket()
            self._reset_state()
            return

        if buy_close and self.Position > 0:
            self.SellMarket()
            self._reset_state()
            return

        if not in_window:
            return

        if self.Position != 0:
            return

        if buy_open:
            self.BuyMarket()
            self._long_entry = close
            self._short_entry = None
        elif sell_open:
            self.SellMarket()
            self._short_entry = close
            self._long_entry = None

    def _calc_roc(self, period):
        if period <= 0 or len(self._close_history) <= period:
            return None
        current = self._close_history[0]
        previous = self._close_history[period]
        # Momentum mode (default)
        return current - previous

    def _try_risk(self, candle):
        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)
        if sl <= 0 and tp <= 0:
            return False

        if self.Position > 0 and self._long_entry is not None:
            if sl > 0 and float(candle.LowPrice) <= self._long_entry - sl:
                self.SellMarket()
                self._reset_state()
                return True
            if tp > 0 and float(candle.HighPrice) >= self._long_entry + tp:
                self.SellMarket()
                self._reset_state()
                return True
        elif self.Position < 0 and self._short_entry is not None:
            if sl > 0 and float(candle.HighPrice) >= self._short_entry + sl:
                self.BuyMarket()
                self._reset_state()
                return True
            if tp > 0 and float(candle.LowPrice) <= self._short_entry - tp:
                self.BuyMarket()
                self._reset_state()
                return True
        return False

    def _in_trade_window(self, time):
        start = TimeSpan(self.StartHour, self.StartMinute, 0)
        end = TimeSpan(self.EndHour, self.EndMinute, 0)
        current = time.TimeOfDay
        if start < end:
            return current >= start and current < end
        if start > end:
            return current >= start or current < end
        return False

    def _reset_state(self):
        self._long_entry = None
        self._short_entry = None

    def OnReseted(self):
        super(xroc2_vg_tm_strategy, self).OnReseted()
        self._close_history = []
        self._fast_history = []
        self._slow_history = []
        self._smooth_fast = None
        self._smooth_slow = None
        self._reset_state()

    def CreateClone(self):
        return xroc2_vg_tm_strategy()
