import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StochasticOscillator, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class j_brain_ultra_rsi_strategy(Strategy):
    # Algorithm modes
    JBRAIN_SIG1_FILTER = 0
    ULTRA_RSI_FILTER = 1
    COMPOSITION = 2

    def __init__(self):
        super(j_brain_ultra_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._stoch_length = self.Param("StochLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Period for %K line", "Indicators")
        self._signal_length = self.Param("SignalLength", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Period for %D line", "Indicators")
        self._mode = self.Param("Mode", 2) \
            .SetDisplay("Mode", "Algorithm to enter the market", "General")
        self._allow_long_entry = self.Param("AllowLongEntry", True) \
            .SetDisplay("Allow Long Entry", "Permission to open long positions", "Trading")
        self._allow_short_entry = self.Param("AllowShortEntry", True) \
            .SetDisplay("Allow Short Entry", "Permission to open short positions", "Trading")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Allow Long Exit", "Permission to close long positions", "Trading")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Allow Short Exit", "Permission to close short positions", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._rsi = None
        self._prev_rsi = None
        self._prev_k = None
        self._prev_d = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def stoch_length(self):
        return self._stoch_length.Value
    @property
    def signal_length(self):
        return self._signal_length.Value
    @property
    def mode(self):
        return self._mode.Value
    @property
    def allow_long_entry(self):
        return self._allow_long_entry.Value
    @property
    def allow_short_entry(self):
        return self._allow_short_entry.Value
    @property
    def allow_long_exit(self):
        return self._allow_long_exit.Value
    @property
    def allow_short_exit(self):
        return self._allow_short_exit.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(j_brain_ultra_rsi_strategy, self).OnReseted()
        self._rsi = None
        self._prev_rsi = None
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(j_brain_ultra_rsi_strategy, self).OnStarted2(time)
        self.StartProtection(None, None)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.stoch_length
        stochastic.D.Length = self.signal_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        # process RSI manually
        rsi_inp = DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.OpenTime)
        rsi_inp.IsFinal = True
        rsi_result = self._rsi.Process(rsi_inp)
        if not rsi_result.IsFormed:
            return

        rsi = float(rsi_result)

        k_val = stoch_value.K
        d_val = stoch_value.D
        if k_val is None or d_val is None:
            self._prev_rsi = rsi
            return

        k = float(k_val)
        d = float(d_val)

        rsi_up = self._prev_rsi is not None and self._prev_rsi <= 50.0 and rsi > 50.0
        rsi_down = self._prev_rsi is not None and self._prev_rsi >= 50.0 and rsi < 50.0
        stoch_up = self._prev_k is not None and self._prev_d is not None and self._prev_k <= self._prev_d and k > d
        stoch_down = self._prev_k is not None and self._prev_d is not None and self._prev_k >= self._prev_d and k < d

        buy_signal = False
        sell_signal = False
        m = self.mode

        if m == self.JBRAIN_SIG1_FILTER:
            buy_signal = rsi_up and k > d
            sell_signal = rsi_down and k < d
        elif m == self.ULTRA_RSI_FILTER:
            buy_signal = stoch_up and rsi > 50.0
            sell_signal = stoch_down and rsi < 50.0
        elif m == self.COMPOSITION:
            buy_signal = rsi_up and stoch_up
            sell_signal = rsi_down and stoch_down

        if buy_signal:
            if self.Position < 0 and self.allow_short_exit:
                self.BuyMarket()
            if self.allow_long_entry and self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position > 0 and self.allow_long_exit:
                self.SellMarket()
            if self.allow_short_entry and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi
        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return j_brain_ultra_rsi_strategy()
