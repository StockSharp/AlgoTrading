import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

class macd_four_colors2_martingale_strategy(Strategy):
    def __init__(self):
        super(macd_four_colors2_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_ema_period = self.Param("FastEmaPeriod", 20)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50)
        self._signal_period = self.Param("SignalPeriod", 12)

        self._macd_history = []
        self._prev_histogram = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @FastEmaPeriod.setter
    def FastEmaPeriod(self, value):
        self._fast_ema_period.Value = value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @SlowEmaPeriod.setter
    def SlowEmaPeriod(self, value):
        self._slow_ema_period.Value = value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    def OnReseted(self):
        super(macd_four_colors2_martingale_strategy, self).OnReseted()
        self._macd_history = []
        self._prev_histogram = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(macd_four_colors2_martingale_strategy, self).OnStarted(time)
        self._macd_history = []
        self._prev_histogram = 0.0
        self._has_prev = False
        self._entry_price = 0.0

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastEmaPeriod
        macd.LongMa.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_value)
        close = float(candle.ClosePrice)
        sig_len = self.SignalPeriod

        self._macd_history.append(macd_val)
        while len(self._macd_history) > sig_len:
            self._macd_history.pop(0)

        if len(self._macd_history) < sig_len:
            return

        signal_val = sum(self._macd_history) / sig_len
        histogram = macd_val - signal_val

        if self._has_prev:
            cross_up = self._prev_histogram < 0 and histogram >= 0
            cross_down = self._prev_histogram >= 0 and histogram < 0

            if cross_up:
                if self.Position < 0:
                    self.BuyMarket()
                    self._entry_price = close
                elif self.Position == 0:
                    self.BuyMarket()
                    self._entry_price = close
            elif cross_down:
                if self.Position > 0:
                    self.SellMarket()
                    self._entry_price = close
                elif self.Position == 0:
                    self.SellMarket()
                    self._entry_price = close

        self._prev_histogram = histogram
        self._has_prev = True

    def CreateClone(self):
        return macd_four_colors2_martingale_strategy()
