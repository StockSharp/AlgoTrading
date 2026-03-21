import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class di_napoli_stochastic_strategy(Strategy):

    def __init__(self):
        super(di_napoli_stochastic_strategy, self).__init__()

        self._fast_k = self.Param("FastK", 8) \
            .SetDisplay("Fast %K", "Base period for %K", "DiNapoli")
        self._slow_d = self.Param("SlowD", 3) \
            .SetDisplay("Slow %D", "%D smoothing period", "DiNapoli")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Type of candles for calculation", "General")

        self._prev_k = 0.0
        self._prev_d = 0.0
        self._prev_ready = False

    @property
    def FastK(self):
        return self._fast_k.Value

    @FastK.setter
    def FastK(self, value):
        self._fast_k.Value = value

    @property
    def SlowD(self):
        return self._slow_d.Value

    @SlowD.setter
    def SlowD(self, value):
        self._slow_d.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(di_napoli_stochastic_strategy, self).OnStarted(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.FastK
        stochastic.D.Length = self.SlowD

        self.SubscribeCandles(self.CandleType) \
            .BindEx(stochastic, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        k_val = stoch_value.K
        d_val = stoch_value.D

        if k_val is None or d_val is None:
            return

        k = float(k_val)
        d = float(d_val)

        if not self._prev_ready:
            self._prev_k = k
            self._prev_d = d
            self._prev_ready = True
            return

        if self._prev_k <= self._prev_d and k > d and self.Position <= 0:


            self.BuyMarket()


        elif self._prev_k >= self._prev_d and k < d and self.Position >= 0:


            self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def OnReseted(self):
        super(di_napoli_stochastic_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._prev_ready = False

    def CreateClone(self):
        return di_napoli_stochastic_strategy()
