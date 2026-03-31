import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class blonde_trader_strategy(Strategy):
    def __init__(self):
        super(blonde_trader_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Period for Highest/Lowest", "General")
        self._threshold = self.Param("Threshold", 0.002) \
            .SetDisplay("Threshold", "Min distance ratio from extreme", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0

    @property
    def lookback(self):
        return self._lookback.Value
    @property
    def threshold(self):
        return self._threshold.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(blonde_trader_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(blonde_trader_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.lookback
        lowest = Lowest()
        lowest.Length = self.lookback
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, high, low):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        high_val = float(high)
        low_val = float(low)
        range_val = high_val - low_val

        if range_val <= 0 or high_val == 0:
            return

        dist_from_high = (high_val - price) / high_val
        dist_from_low = (price - low_val) / price

        if dist_from_high > self.threshold and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = price
        elif dist_from_low > self.threshold and self.Position >= 0:
            self.SellMarket()
            self._entry_price = price

    def CreateClone(self):
        return blonde_trader_strategy()
