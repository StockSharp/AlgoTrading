import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class external_level_strategy(Strategy):
    def __init__(self):
        super(external_level_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Support/resistance lookback period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Source candle timeframe", "General")
        self._prev_resistance = 0.0
        self._prev_support = 0.0

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(external_level_strategy, self).OnReseted()
        self._prev_resistance = 0.0
        self._prev_support = 0.0

    def OnStarted2(self, time):
        super(external_level_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.lookback
        lowest = Lowest()
        lowest.Length = self.lookback
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, resistance, support):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_resistance == 0:
            self._prev_resistance = resistance
            self._prev_support = support
            return
        if candle.ClosePrice > self._prev_resistance and self.Position <= 0:
            self.BuyMarket()
        elif candle.ClosePrice < self._prev_support and self.Position >= 0:
            self.SellMarket()
        self._prev_resistance = resistance
        self._prev_support = support

    def CreateClone(self):
        return external_level_strategy()
