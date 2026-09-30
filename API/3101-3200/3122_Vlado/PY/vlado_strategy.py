import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class vlado_strategy(Strategy):
    def __init__(self):
        super(vlado_strategy, self).__init__()

        self._williams_period = self.Param("WilliamsPeriod", 14).SetGreaterThanZero()
        self._overbought_level = self.Param("OverboughtLevel", -25.0)
        self._oversold_level = self.Param("OversoldLevel", -75.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._williams_r = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnStarted2(self, time):
        super(vlado_strategy, self).OnStarted2(time)

        self._williams_r = WilliamsR()
        self._williams_r.Length = int(self._williams_period.Value)

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self._williams_r, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            williams_area = self.CreateChartArea()
            if williams_area is not None:
                self.DrawIndicator(williams_area, self._williams_r)

    def _process_candle(self, candle, williams_value):
        if candle.State != CandleStates.Finished or not self._williams_r.IsFormed:
            return

        value = float(williams_value)

        signal = self.get_signal(value, float(self._oversold_level.Value), float(self._overbought_level.Value))

        if signal > 0 and self.Position <= 0:
            self.LogInfo("Williams %R {0}: LONG", value)
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.LogInfo("Williams %R {0}: SHORT", value)
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    @staticmethod
    def get_signal(williams_r, oversold_level, overbought_level):
        if williams_r <= oversold_level:
            return 1
        if williams_r >= overbought_level:
            return -1
        return 0

    def CreateClone(self):
        return vlado_strategy()
