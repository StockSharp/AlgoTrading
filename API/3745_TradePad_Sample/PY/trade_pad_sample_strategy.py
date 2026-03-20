import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class trade_pad_sample_strategy(Strategy):
    """Stochastic crossover strategy. Buys when %K crosses up from oversold, sells when it crosses down from overbought."""

    def __init__(self):
        super(trade_pad_sample_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 10) \
            .SetDisplay("Stochastic %K", "%K period", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Stochastic %D", "%D period", "Indicators")
        self._upper_level = self.Param("UpperLevel", 75.0) \
            .SetDisplay("Upper Threshold", "Overbought level", "Signals")
        self._lower_level = self.Param("LowerLevel", 25.0) \
            .SetDisplay("Lower Threshold", "Oversold level", "Signals")

        self._prev_k = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @property
    def UpperLevel(self):
        return self._upper_level.Value

    @property
    def LowerLevel(self):
        return self._lower_level.Value

    def OnReseted(self):
        super(trade_pad_sample_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(trade_pad_sample_strategy, self).OnStarted(time)

        self._has_prev = False

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochasticKPeriod
        stochastic.D.Length = self.StochasticDPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, self._process_candle).Start()

    def _process_candle(self, candle, stoch_val):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_val.IsFinal or not stoch_val.IsFormed:
            return

        k_raw = stoch_val.K
        if k_raw is None:
            return

        k_value = float(k_raw)

        if not self._has_prev:
            self._prev_k = k_value
            self._has_prev = True
            return

        lower = float(self.LowerLevel)
        upper = float(self.UpperLevel)

        cross_up = self._prev_k <= lower and k_value > lower
        cross_down = self._prev_k >= upper and k_value < upper

        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

        self._prev_k = k_value

    def CreateClone(self):
        return trade_pad_sample_strategy()
