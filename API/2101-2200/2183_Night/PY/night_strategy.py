import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class night_strategy(Strategy):
    def __init__(self):
        super(night_strategy, self).__init__()
        self._stoch_oversold = self.Param("StochOversold", 30.0) \
            .SetDisplay("Stochastic Oversold", "Oversold level for %K", "Indicators")
        self._stoch_overbought = self.Param("StochOverbought", 70.0) \
            .SetDisplay("Stochastic Overbought", "Overbought level for %K", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

    @property
    def stoch_oversold(self):
        return self._stoch_oversold.Value

    @property
    def stoch_overbought(self):
        return self._stoch_overbought.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(night_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(night_strategy, self).OnStarted2(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = 14
        stochastic.D.Length = 3

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
        if k is None:
            return
        k_value = float(k)

        # Trade only during night hours 21:00-06:00
        hour = candle.OpenTime.Hour
        is_night = hour >= 21 or hour < 6
        if not is_night:
            return

        if k_value < float(self.stoch_oversold) and self.Position <= 0:
            self.BuyMarket()
        elif k_value > float(self.stoch_overbought) and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return night_strategy()
