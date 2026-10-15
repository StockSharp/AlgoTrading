import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class blau_ergodic_mdi_time_strategy(Strategy):
    """Blau Ergodic MDI with time filter. Computes a custom triple-smoothed
    momentum oscillator and generates signals via Twist mode (slope reversal)."""

    def __init__(self):
        super(blau_ergodic_mdi_time_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._base_length = self.Param("BaseLength", 20) \
            .SetDisplay("Base Length", "Length of the base EMA", "Indicator")
        self._first_smooth = self.Param("FirstSmooth", 5) \
            .SetDisplay("First Smooth", "Length of the first smoothing", "Indicator")
        self._second_smooth = self.Param("SecondSmooth", 3) \
            .SetDisplay("Second Smooth", "Length of the second smoothing", "Indicator")
        self._third_smooth = self.Param("ThirdSmooth", 8) \
            .SetDisplay("Third Smooth", "Length of the third smoothing", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Number of bars back used for the signal", "Indicator")
        self._use_time_filter = self.Param("UseTimeFilter", True) \
            .SetDisplay("Use Time Filter", "Restrict trading to configured session", "Time Filter")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Hour when trading can start", "Time Filter")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Hour when trading stops", "Time Filter")

        self._price_ema = None
        self._diff_ema1 = None
        self._diff_ema2 = None
        self._diff_ema3 = None
        self._hist_buffer = []
        self._bars_processed = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def BaseLength(self):
        return self._base_length.Value

    @property
    def FirstSmooth(self):
        return self._first_smooth.Value

    @property
    def SecondSmooth(self):
        return self._second_smooth.Value

    @property
    def ThirdSmooth(self):
        return self._third_smooth.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @property
    def UseTimeFilter(self):
        return self._use_time_filter.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    def OnReseted(self):
        super(blau_ergodic_mdi_time_strategy, self).OnReseted()
        self._price_ema = None
        self._diff_ema1 = None
        self._diff_ema2 = None
        self._diff_ema3 = None
        self._hist_buffer = []
        self._bars_processed = 0

    def OnStarted2(self, time):
        super(blau_ergodic_mdi_time_strategy, self).OnStarted2(time)
        self._price_ema = None
        self._diff_ema1 = None
        self._diff_ema2 = None
        self._diff_ema3 = None
        self._hist_buffer = []
        self._bars_processed = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

    def _update_ema(self, previous, value, length):
        if length <= 1:
            return value
        alpha = 2.0 / (length + 1.0)
        if previous is None:
            return value
        return previous + alpha * (value - previous)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)

        base_len = max(1, self.BaseLength)
        first_len = max(1, self.FirstSmooth)
        second_len = max(1, self.SecondSmooth)
        third_len = max(1, self.ThirdSmooth)

        base_smoothed = self._update_ema(self._price_ema, price, base_len)
        self._price_ema = base_smoothed

        diff = price - base_smoothed
        diff_s1 = self._update_ema(self._diff_ema1, diff, first_len)
        self._diff_ema1 = diff_s1

        diff_s2 = self._update_ema(self._diff_ema2, diff_s1, second_len)
        self._diff_ema2 = diff_s2

        diff_s3 = self._update_ema(self._diff_ema3, diff_s2, third_len)
        self._diff_ema3 = diff_s3

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        hist_value = diff_s2 / step

        self._bars_processed += 1
        signal_bar = max(0, self.SignalBar)
        required = signal_bar + 3

        self._hist_buffer.insert(0, hist_value)
        if len(self._hist_buffer) > required:
            self._hist_buffer = self._hist_buffer[:required]

        minimum_bars = base_len + first_len + second_len + third_len + signal_bar + 3
        if self._bars_processed < minimum_bars:
            return

        if self.UseTimeFilter:
            hour = candle.OpenTime.Hour
            if not (self.StartHour <= hour <= self.EndHour):
                if self.Position != 0:
                    if self.Position > 0:
                        self.SellMarket()
                    else:
                        self.BuyMarket()
                return

        if len(self._hist_buffer) < signal_bar + 3:
            return

        current = self._hist_buffer[signal_bar]
        prev1 = self._hist_buffer[signal_bar + 1]
        prev2 = self._hist_buffer[signal_bar + 2]

        if prev1 < prev2 and current > prev1 and self.Position <= 0:
            self.BuyMarket()
        elif prev1 > prev2 and current < prev1 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return blau_ergodic_mdi_time_strategy()
