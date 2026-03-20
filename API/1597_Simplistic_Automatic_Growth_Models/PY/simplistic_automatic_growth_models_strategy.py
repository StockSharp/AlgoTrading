import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class simplistic_automatic_growth_models_strategy(Strategy):
    def __init__(self):
        super(simplistic_automatic_growth_models_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")
        self._length = self.Param("Length", 10) \
            .SetDisplay("Length", "Lookback length for bands", "Indicators")
        self._cum_high = 0.0
        self._cum_low = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def length(self):
        return self._length.Value

    def OnReseted(self):
        super(simplistic_automatic_growth_models_strategy, self).OnReseted()
        self._cum_high = 0.0
        self._cum_low = 0.0
        self._count = 0

    def OnStarted(self, time):
        super(simplistic_automatic_growth_models_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.length
        lowest = Lowest()
        lowest.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, hi, lo):
        if candle.State != CandleStates.Finished:
            return
        self._cum_high += hi
        self._cum_low += lo
        self._count += 1
        avg_hi = self._cum_high / self._count
        avg_lo = self._cum_low / self._count
        if candle.ClosePrice > avg_hi and self.Position <= 0:
            self.BuyMarket()
        elif candle.ClosePrice < avg_lo and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return simplistic_automatic_growth_models_strategy()
