import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class stochastic_strategy(Strategy):
    def __init__(self):
        super(stochastic_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14).SetGreaterThanZero()
        self._over_sold = self.Param("OverSold", 50.0)
        self._over_bought = self.Param("OverBought", 50.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._candles = []
        self._previous_k = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(stochastic_strategy, self).OnReseted()
        self._candles = []
        self._previous_k = None

    def OnStarted2(self, time):
        super(stochastic_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        period = int(self._k_period.Value)
        self._candles.append(candle)
        if len(self._candles) > period:
            del self._candles[0]

        if len(self._candles) < period:
            return

        high = max(float(c.HighPrice) for c in self._candles)
        low = min(float(c.LowPrice) for c in self._candles)
        current_k = 50.0 if high == low else (float(candle.ClosePrice) - low) / (high - low) * 100.0

        if self._previous_k is not None:
            signal = self.get_signal(
                self._previous_k, current_k,
                float(self._over_sold.Value), float(self._over_bought.Value))

            if signal > 0 and self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif signal < 0 and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._previous_k = current_k

    @staticmethod
    def get_signal(previous_k, current_k, over_sold, over_bought):
        if previous_k <= over_sold and current_k > over_sold:
            return 1
        if previous_k >= over_bought and current_k < over_bought:
            return -1
        return 0

    def CreateClone(self):
        return stochastic_strategy()
