import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class zz_fibo_trader_strategy(Strategy):
    def __init__(self):
        super(zz_fibo_trader_strategy, self).__init__()
        self._zigzag_depth = self.Param("ZigZagDepth", 12) \
            .SetDisplay("ZigZag Depth", "Number of bars to search for pivots", "ZigZag")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_pivot = 0.0
        self._curr_pivot = 0.0
        self._direction = 0
        self._level50 = 0.0

    @property
    def zigzag_depth(self):
        return self._zigzag_depth.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zz_fibo_trader_strategy, self).OnReseted()
        self._prev_pivot = 0.0
        self._curr_pivot = 0.0
        self._direction = 0
        self._level50 = 0.0

    def OnStarted(self, time):
        super(zz_fibo_trader_strategy, self).OnStarted(time)
        self._prev_pivot = 0.0
        self._curr_pivot = 0.0
        self._direction = 0
        self._level50 = 0.0
        highest = Highest()
        highest.Length = self.zigzag_depth
        lowest = Lowest()
        lowest.Length = self.zigzag_depth
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _update_levels(self):
        if self._prev_pivot == 0.0 or self._curr_pivot == 0.0:
            return
        self._direction = 1 if self._curr_pivot > self._prev_pivot else -1
        if self._direction == 1:
            high = self._curr_pivot
            low = self._prev_pivot
        else:
            high = self._prev_pivot
            low = self._curr_pivot
        self._level50 = high - (high - low) * 0.5

    def process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        highest = float(highest)
        lowest = float(lowest)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        close_price = float(candle.ClosePrice)
        if high_price >= highest and high_price != self._curr_pivot:
            self._prev_pivot = self._curr_pivot
            self._curr_pivot = high_price
            self._update_levels()
        elif low_price <= lowest and low_price != self._curr_pivot:
            self._prev_pivot = self._curr_pivot
            self._curr_pivot = low_price
            self._update_levels()
        if self._direction == 0 or self._level50 == 0.0:
            return
        if self._direction == 1 and self.Position <= 0 and close_price > self._level50:
            self.BuyMarket()
        elif self._direction == -1 and self.Position >= 0 and close_price < self._level50:
            self.SellMarket()

    def CreateClone(self):
        return zz_fibo_trader_strategy()
