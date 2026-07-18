import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class trend_arrows_strategy(Strategy):
    def __init__(self):
        super(trend_arrows_strategy, self).__init__()
        self._period = self.Param("Period", 15) \
            .SetDisplay("Period", "Number of bars for extreme calculation", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe of candles", "Parameters")
        self._prev_trend_up = False
        self._prev_trend_down = False
        self._prev_highest = None
        self._prev_lowest = None

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_arrows_strategy, self).OnReseted()
        self._prev_trend_up = False
        self._prev_trend_down = False
        self._prev_highest = None
        self._prev_lowest = None

    def OnStarted2(self, time):
        super(trend_arrows_strategy, self).OnStarted2(time)
        self._prev_trend_up = False
        self._prev_trend_down = False
        self._prev_highest = None
        self._prev_lowest = None
        highest = Highest()
        highest.Length = self.period
        lowest = Lowest()
        lowest.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return
        highest_val = float(highest_val)
        lowest_val = float(lowest_val)
        close_price = float(candle.ClosePrice)
        if self._prev_highest is None or self._prev_lowest is None:
            self._prev_highest = highest_val
            self._prev_lowest = lowest_val
            return
        trend_up = False
        trend_down = False
        if close_price > self._prev_highest:
            trend_up = True
        elif close_price < self._prev_lowest:
            trend_down = True
        else:
            trend_up = self._prev_trend_up
            trend_down = self._prev_trend_down
        if not self._prev_trend_up and trend_up and self.Position <= 0:
            self.BuyMarket()
        if not self._prev_trend_down and trend_down and self.Position >= 0:
            self.SellMarket()
        self._prev_trend_up = trend_up
        self._prev_trend_down = trend_down
        self._prev_highest = highest_val
        self._prev_lowest = lowest_val

    def CreateClone(self):
        return trend_arrows_strategy()
