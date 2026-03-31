import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class altarius_rsi_stochastic_dual_strategy(Strategy):
    """
    Strategy converted from AltariusRSIxampnSTOH MQL4 expert advisor.
    Combines dual stochastic filters with RSI based exits and dynamic position sizing.
    """

    def __init__(self):
        super(altarius_rsi_stochastic_dual_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 1.0) \
            .SetDisplay("Base Volume", "Initial volume before money management rules", "Trading")
        self._rsi_period = self.Param("RsiPeriod", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Length of RSI used for exits", "Indicators")
        self._slow_stoch_k = self.Param("SlowStochasticK", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Stochastic %K", "Smoothing of %K for slow stochastic", "Indicators")
        self._slow_stoch_d = self.Param("SlowStochasticD", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Stochastic %D", "Smoothing of %D for slow stochastic", "Indicators")
        self._fast_stoch_k = self.Param("FastStochasticK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Stochastic %K", "Smoothing of %K for fast stochastic", "Indicators")
        self._fast_stoch_d = self.Param("FastStochasticD", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Stochastic %D", "Smoothing of %D for fast stochastic", "Indicators")
        self._diff_threshold = self.Param("StochasticDifferenceThreshold", 5.0) \
            .SetDisplay("Momentum Threshold", "Minimum difference between fast stochastic lines", "Trading")
        self._buy_limit = self.Param("BuyStochasticLimit", 50.0) \
            .SetDisplay("Buy Stochastic Limit", "Upper bound of slow stochastic for longs", "Trading")
        self._sell_limit = self.Param("SellStochasticLimit", 55.0) \
            .SetDisplay("Sell Stochastic Limit", "Lower bound of slow stochastic for shorts", "Trading")
        self._exit_rsi_high = self.Param("ExitRsiHigh", 60.0) \
            .SetDisplay("Exit RSI High", "RSI threshold to exit longs", "Exits")
        self._exit_rsi_low = self.Param("ExitRsiLow", 40.0) \
            .SetDisplay("Exit RSI Low", "RSI threshold to exit shorts", "Exits")
        self._exit_stoch_high = self.Param("ExitStochasticHigh", 70.0) \
            .SetDisplay("Exit Stochastic High", "Slow stochastic signal level confirming long exit", "Exits")
        self._exit_stoch_low = self.Param("ExitStochasticLow", 30.0) \
            .SetDisplay("Exit Stochastic Low", "Slow stochastic signal level confirming short exit", "Exits")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles used for calculations", "Market Data")

        self._prev_slow_signal = 0.0
        self._has_prev_slow_signal = False

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(altarius_rsi_stochastic_dual_strategy, self).OnReseted()
        self._prev_slow_signal = 0.0
        self._has_prev_slow_signal = False

    def OnStarted2(self, time):
        super(altarius_rsi_stochastic_dual_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        slow_stoch = StochasticOscillator()
        slow_stoch.K.Length = self._slow_stoch_k.Value
        slow_stoch.D.Length = self._slow_stoch_d.Value

        fast_stoch = StochasticOscillator()
        fast_stoch.K.Length = self._fast_stoch_k.Value
        fast_stoch.D.Length = self._fast_stoch_d.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(rsi, slow_stoch, fast_stoch, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, slow_value, fast_value):
        if candle.State != CandleStates.Finished:
            return
        if rsi_value.IsEmpty or slow_value.IsEmpty or fast_value.IsEmpty:
            return

        rsi = float(rsi_value)

        slow_k_raw = slow_value.K if hasattr(slow_value, 'K') else None
        slow_d_raw = slow_value.D if hasattr(slow_value, 'D') else None
        fast_k_raw = fast_value.K if hasattr(fast_value, 'K') else None
        fast_d_raw = fast_value.D if hasattr(fast_value, 'D') else None

        if slow_k_raw is None or slow_d_raw is None or fast_k_raw is None or fast_d_raw is None:
            return

        slow_k = float(slow_k_raw)
        slow_d = float(slow_d_raw)
        fast_k = float(fast_k_raw)
        fast_d = float(fast_d_raw)

        if not self._has_prev_slow_signal:
            self._prev_slow_signal = slow_d
            self._has_prev_slow_signal = True
            return

        if self.Position == 0:
            momentum = abs(fast_k - fast_d)
            if slow_k > slow_d and slow_k < self._buy_limit.Value and momentum > self._diff_threshold.Value:
                self.BuyMarket(self.Volume)
            elif slow_k < slow_d and slow_k > self._sell_limit.Value and momentum > self._diff_threshold.Value:
                self.SellMarket(self.Volume)
        elif self.Position > 0:
            if rsi > self._exit_rsi_high.Value and slow_d < self._prev_slow_signal and slow_d > self._exit_stoch_high.Value:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if rsi < self._exit_rsi_low.Value and slow_d > self._prev_slow_signal and slow_d < self._exit_stoch_low.Value:
                self.BuyMarket(Math.Abs(self.Position))

        self._prev_slow_signal = slow_d

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return altarius_rsi_stochastic_dual_strategy()
