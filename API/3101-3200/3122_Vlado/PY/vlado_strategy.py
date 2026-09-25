import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class vlado_strategy(Strategy):
    def __init__(self):
        super(vlado_strategy, self).__init__()

        self._williams_period = self.Param("WilliamsPeriod", 14).SetGreaterThanZero()
        self._overbought_level = self.Param("OverboughtLevel", -25.0)
        self._oversold_level = self.Param("OversoldLevel", -75.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._candles = []

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(vlado_strategy, self).OnReseted()
        self._candles = []

    def OnStarted2(self, time):
        super(vlado_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candles.append(candle)
        period = int(self._williams_period.Value)
        if len(self._candles) > period:
            del self._candles[:-period]

        if len(self._candles) < period:
            return

        highest = max(float(c.HighPrice) for c in self._candles)
        lowest = min(float(c.LowPrice) for c in self._candles)
        value = -50.0 if highest == lowest else -100.0 * (highest - float(candle.ClosePrice)) / (highest - lowest)

        signal = self.get_signal(value, float(self._oversold_level.Value), float(self._overbought_level.Value))

        if signal > 0 and self.Position <= 0:
            self.LogInfo("Williams %R {0}: LONG", value)
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.LogInfo("Williams %R {0}: SHORT", value)
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    @staticmethod
    def get_signal(williams_r, oversold_level, overbought_level):
        if williams_r < oversold_level:
            return 1
        if williams_r > overbought_level:
            return -1
        return 0

    def CreateClone(self):
        return vlado_strategy()
