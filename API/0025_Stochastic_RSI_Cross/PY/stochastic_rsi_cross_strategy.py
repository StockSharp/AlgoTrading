import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class stochastic_rsi_cross_strategy(Strategy):
    """
    Stochastic K/D crossover strategy.
    Buys when %K crosses above %D in oversold zone.
    Sells when %K crosses below %D in overbought zone.
    """

    def __init__(self):
        super(stochastic_rsi_cross_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14).SetDisplay("K Period", "Period for %K line", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetDisplay("D Period", "Period for %D line", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_k = 0.0
        self._prev_d = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_rsi_cross_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(stochastic_rsi_cross_strategy, self).OnStarted(time)

        stoch = StochasticOscillator()
        stoch.K.Length = self._k_period.Value
        stoch.D.Length = self._d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, stoch_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if stoch_val.K is None or stoch_val.D is None:
            return

        k = float(stoch_val.K)
        d = float(stoch_val.D)

        if not self._has_prev:
            self._has_prev = True
            self._prev_k = k
            self._prev_d = d
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_k = k
            self._prev_d = d
            return

        # %K crosses above %D in oversold zone (< 20)
        if self._prev_k <= self._prev_d and k > d and k < 20 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 5
        # %K crosses below %D in overbought zone (> 80)
        elif self._prev_k >= self._prev_d and k < d and k > 80 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 5

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return stochastic_rsi_cross_strategy()
