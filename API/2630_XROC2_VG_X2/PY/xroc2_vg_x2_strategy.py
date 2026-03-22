import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue


class _RocSmoother(object):
    """Computes ROC then smooths with EMA."""

    def __init__(self, period, length):
        self._period = max(1, period)
        self._smoother = ExponentialMovingAverage()
        self._smoother.Length = max(1, length)
        self._window = []

    def process(self, close, time):
        self._window.append(close)
        if len(self._window) < self._period + 1:
            return None
        while len(self._window) > self._period + 1:
            self._window.pop(0)

        prev = self._window[0]
        roc = close - prev  # momentum mode

        out = self._smoother.Process(DecimalIndicatorValue(self._smoother, roc, time))
        try:
            return float(out)
        except Exception:
            return None


class _Xroc2VgSeries(object):
    """Manages fast/slow ROC smoothers and their signal history."""

    def __init__(self, fast_period, fast_length, slow_period, slow_length):
        self._fast = _RocSmoother(fast_period, fast_length)
        self._slow = _RocSmoother(slow_period, slow_length)
        self._history = []

    def process(self, candle):
        close = float(candle.ClosePrice)
        t = candle.OpenTime
        fast = self._fast.process(close, t)
        slow = self._slow.process(close, t)
        if fast is None or slow is None:
            return False
        self._history.append((fast, slow))
        if len(self._history) > 1024:
            self._history.pop(0)
        return True

    def try_get_value(self, signal_bar):
        if signal_bar <= 0:
            return None
        idx = len(self._history) - signal_bar
        if idx < 0 or idx >= len(self._history):
            return None
        return self._history[idx]

    def try_get_pair(self, signal_bar):
        if signal_bar <= 0:
            return None, None
        idx = len(self._history) - signal_bar
        if idx < 1 or idx >= len(self._history):
            return None, None
        return self._history[idx], self._history[idx - 1]


class xroc2_vg_x2_strategy(Strategy):
    """Multi-timeframe XROC2 VG: higher TF for bias, lower TF for entries."""

    def __init__(self):
        super(xroc2_vg_x2_strategy, self).__init__()

        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Higher TF", "Higher timeframe candles", "General")
        self._lower_candle_type = self.Param("LowerCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Lower TF", "Lower timeframe candles", "General")
        self._higher_signal_bar = self.Param("HigherSignalBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Signal Bar", "Shift for trend evaluation", "General")
        self._lower_signal_bar = self.Param("LowerSignalBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Signal Bar", "Shift for lower TF signals", "General")
        self._higher_fast_period = self.Param("HigherFastPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Fast ROC", "Fast ROC period for bias", "Higher TF")
        self._higher_fast_length = self.Param("HigherFastLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Fast Length", "Length of fast smoother", "Higher TF")
        self._higher_slow_period = self.Param("HigherSlowPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Slow ROC", "Slow ROC period for bias", "Higher TF")
        self._higher_slow_length = self.Param("HigherSlowLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Higher Slow Length", "Length of slow smoother", "Higher TF")
        self._lower_fast_period = self.Param("LowerFastPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Fast ROC", "Fast ROC period for entries", "Lower TF")
        self._lower_fast_length = self.Param("LowerFastLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Fast Length", "Length of fast smoother", "Lower TF")
        self._lower_slow_period = self.Param("LowerSlowPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Slow ROC", "Slow ROC period for entries", "Lower TF")
        self._lower_slow_length = self.Param("LowerSlowLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lower Slow Length", "Length of slow smoother", "Lower TF")
        self._allow_buy = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Long Entries", "Enable long entries", "Signals")
        self._allow_sell = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Short Entries", "Enable short entries", "Signals")
        self._close_buy_trend = self.Param("CloseBuyOnTrendFlip", True) \
            .SetDisplay("Close Long On Trend", "Close longs when trend turns bearish", "Signals")
        self._close_sell_trend = self.Param("CloseSellOnTrendFlip", True) \
            .SetDisplay("Close Short On Trend", "Close shorts when trend turns bullish", "Signals")
        self._close_buy_lower = self.Param("CloseBuyOnLower", True) \
            .SetDisplay("Close Long On Lower", "Close longs when lower crosses down", "Signals")
        self._close_sell_lower = self.Param("CloseSellOnLower", True) \
            .SetDisplay("Close Short On Lower", "Close shorts when lower crosses up", "Signals")

        self._higher_series = None
        self._lower_series = None
        self._trend = 0

    @property
    def HigherCandleType(self):
        return self._higher_candle_type.Value
    @property
    def LowerCandleType(self):
        return self._lower_candle_type.Value
    @property
    def HigherSignalBar(self):
        return self._higher_signal_bar.Value
    @property
    def LowerSignalBar(self):
        return self._lower_signal_bar.Value
    @property
    def HigherFastPeriod(self):
        return self._higher_fast_period.Value
    @property
    def HigherFastLength(self):
        return self._higher_fast_length.Value
    @property
    def HigherSlowPeriod(self):
        return self._higher_slow_period.Value
    @property
    def HigherSlowLength(self):
        return self._higher_slow_length.Value
    @property
    def LowerFastPeriod(self):
        return self._lower_fast_period.Value
    @property
    def LowerFastLength(self):
        return self._lower_fast_length.Value
    @property
    def LowerSlowPeriod(self):
        return self._lower_slow_period.Value
    @property
    def LowerSlowLength(self):
        return self._lower_slow_length.Value
    @property
    def AllowBuyOpen(self):
        return self._allow_buy.Value
    @property
    def AllowSellOpen(self):
        return self._allow_sell.Value
    @property
    def CloseBuyOnTrendFlip(self):
        return self._close_buy_trend.Value
    @property
    def CloseSellOnTrendFlip(self):
        return self._close_sell_trend.Value
    @property
    def CloseBuyOnLower(self):
        return self._close_buy_lower.Value
    @property
    def CloseSellOnLower(self):
        return self._close_sell_lower.Value

    def OnStarted(self, time):
        super(xroc2_vg_x2_strategy, self).OnStarted(time)

        self._higher_series = _Xroc2VgSeries(
            self.HigherFastPeriod, self.HigherFastLength,
            self.HigherSlowPeriod, self.HigherSlowLength)
        self._lower_series = _Xroc2VgSeries(
            self.LowerFastPeriod, self.LowerFastLength,
            self.LowerSlowPeriod, self.LowerSlowLength)
        self._trend = 0

        higher_sub = self.SubscribeCandles(self.HigherCandleType)
        higher_sub.Bind(self._process_higher).Start()

        lower_sub = self.SubscribeCandles(self.LowerCandleType)
        lower_sub.Bind(self._process_lower).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, lower_sub)
            self.DrawOwnTrades(area)

    def _process_higher(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self._higher_series.process(candle):
            return
        val = self._higher_series.try_get_value(self.HigherSignalBar)
        if val is not None:
            if val[0] > val[1]:
                self._trend = 1
            elif val[0] < val[1]:
                self._trend = -1
            else:
                self._trend = 0

    def _process_lower(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self._lower_series.process(candle):
            return

        current, previous = self._lower_series.try_get_pair(self.LowerSignalBar)
        if current is None or previous is None:
            return

        if self._trend == 0:
            return

        buy_close = self.CloseBuyOnLower and previous[0] < previous[1]
        sell_close = self.CloseSellOnLower and previous[0] > previous[1]

        if self._trend < 0 and self.CloseBuyOnTrendFlip:
            buy_close = True
        if self._trend > 0 and self.CloseSellOnTrendFlip:
            sell_close = True

        buy_open = self._trend > 0 and self.AllowBuyOpen and current[0] <= current[1] and previous[0] > previous[1]
        sell_open = self._trend < 0 and self.AllowSellOpen and current[0] >= current[1] and previous[0] < previous[1]

        pos = self.Position

        if buy_close and pos > 0:
            self.SellMarket()
            pos = self.Position

        if sell_close and pos < 0:
            self.BuyMarket()
            pos = self.Position

        if buy_open and pos == 0:
            self.BuyMarket()
            return

        if sell_open and pos == 0:
            self.SellMarket()

    def OnReseted(self):
        super(xroc2_vg_x2_strategy, self).OnReseted()
        self._higher_series = None
        self._lower_series = None
        self._trend = 0

    def CreateClone(self):
        return xroc2_vg_x2_strategy()
