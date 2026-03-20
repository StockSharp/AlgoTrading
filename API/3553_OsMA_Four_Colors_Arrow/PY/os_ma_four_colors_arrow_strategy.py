import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

class os_ma_four_colors_arrow_strategy(Strategy):
    def __init__(self):
        super(os_ma_four_colors_arrow_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 20)
        self._slow_period = self.Param("SlowPeriod", 50)
        self._signal_period = self.Param("SignalPeriod", 12)

        self._macd_history = []
        self._prev_histogram = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    def OnReseted(self):
        super(os_ma_four_colors_arrow_strategy, self).OnReseted()
        self._macd_history = []
        self._prev_histogram = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(os_ma_four_colors_arrow_strategy, self).OnStarted(time)
        self._macd_history = []
        self._prev_histogram = 0.0
        self._has_prev = False

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastPeriod
        macd.LongMa.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_value)
        sig_len = self.SignalPeriod

        self._macd_history.append(macd_val)
        while len(self._macd_history) > sig_len:
            self._macd_history.pop(0)

        if len(self._macd_history) < sig_len:
            return

        signal_val = sum(self._macd_history) / sig_len
        histogram = macd_val - signal_val

        if self._has_prev:
            cross_up = self._prev_histogram <= 0 and histogram > 0
            cross_down = self._prev_histogram >= 0 and histogram < 0

            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_histogram = histogram
        self._has_prev = True

    def CreateClone(self):
        return os_ma_four_colors_arrow_strategy()
