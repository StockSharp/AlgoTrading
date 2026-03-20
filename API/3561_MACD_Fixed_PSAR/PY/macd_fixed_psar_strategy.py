import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_fixed_psar_strategy(Strategy):
    def __init__(self):
        super(macd_fixed_psar_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 20)
        self._slow_period = self.Param("SlowPeriod", 50)
        self._signal_period = self.Param("SignalPeriod", 12)
        self._trend_period = self.Param("TrendPeriod", 60)

        self._macd_history = []
        self._prev_histogram = None

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

    @property
    def TrendPeriod(self):
        return self._trend_period.Value

    @TrendPeriod.setter
    def TrendPeriod(self, value):
        self._trend_period.Value = value

    def OnReseted(self):
        super(macd_fixed_psar_strategy, self).OnReseted()
        self._macd_history = []
        self._prev_histogram = None

    def OnStarted(self, time):
        super(macd_fixed_psar_strategy, self).OnStarted(time)
        self._macd_history = []
        self._prev_histogram = None

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastPeriod
        macd.LongMa.Length = self.SlowPeriod
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self.TrendPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, trend_ema, self._process_candle).Start()

    def _process_candle(self, candle, macd_value, trend_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_value)
        trend_val = float(trend_value)
        close = float(candle.ClosePrice)
        signal_period = self.SignalPeriod

        self._macd_history.append(macd_val)
        while len(self._macd_history) > signal_period:
            self._macd_history.pop(0)

        if len(self._macd_history) < signal_period:
            return

        signal = sum(self._macd_history) / signal_period
        histogram = macd_val - signal

        if self._prev_histogram is None:
            self._prev_histogram = histogram
            return

        cross_up = self._prev_histogram <= 0 and histogram > 0
        cross_down = self._prev_histogram >= 0 and histogram < 0

        if cross_up and close > trend_val:
            if self.Position <= 0:
                self.BuyMarket()
        elif cross_down and close < trend_val:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_histogram = histogram

    def CreateClone(self):
        return macd_fixed_psar_strategy()
