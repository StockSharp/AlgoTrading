import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class stochastic_automated_strategy(Strategy):
    def __init__(self):
        super(stochastic_automated_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("%K Period", "Stochastic %K period", "Stochastic")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("%D Period", "Stochastic %D period", "Stochastic")
        self._over_bought = self.Param("OverBought", 80.0) \
            .SetDisplay("Overbought", "Overbought threshold", "Stochastic")
        self._over_sold = self.Param("OverSold", 20.0) \
            .SetDisplay("Oversold", "Oversold threshold", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame", "General")
        self._prev_k = None
        self._prev_d = None

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def over_bought(self):
        return self._over_bought.Value

    @property
    def over_sold(self):
        return self._over_sold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_automated_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(stochastic_automated_strategy, self).OnStarted2(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.k_period
        stochastic.D.Length = self.d_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        k = stoch_value.K
        d = stoch_value.D
        if k is None or d is None:
            return
        k = float(k)
        d = float(d)

        if self._prev_k is not None and self._prev_d is not None:
            ob = float(self.over_bought)
            os_val = float(self.over_sold)

            # Buy: K crosses above D in oversold zone
            if self._prev_k <= self._prev_d and k > d and self._prev_d < os_val and self.Position <= 0:
                self.BuyMarket()

            # Sell: K crosses below D in overbought zone
            if self._prev_k >= self._prev_d and k < d and self._prev_d > ob and self.Position >= 0:
                self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return stochastic_automated_strategy()
