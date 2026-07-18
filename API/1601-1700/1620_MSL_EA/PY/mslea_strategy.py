import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class mslea_strategy(Strategy):
    def __init__(self):
        super(mslea_strategy, self).__init__()
        self._max_trades = self.Param("MaxTrades", 2) \
            .SetDisplay("Max Trades", "Maximum simultaneous trades", "General")
        self._level = self.Param("Level", 1) \
            .SetDisplay("Level", "Number of extremes to look back", "General")
        self._distance = self.Param("Distance", 4) \
            .SetDisplay("Distance", "Offset from extreme in ticks", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._high_levels = []
        self._low_levels = []
        self._prev_high1 = None
        self._prev_high2 = None
        self._prev_low1 = None
        self._prev_low2 = None
        self._msh = None
        self._msl = None

    @property
    def max_trades(self):
        return self._max_trades.Value

    @property
    def level(self):
        return self._level.Value

    @property
    def distance(self):
        return self._distance.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mslea_strategy, self).OnReseted()
        self._high_levels = []
        self._low_levels = []
        self._prev_high1 = None
        self._prev_high2 = None
        self._prev_low1 = None
        self._prev_low2 = None
        self._msh = None
        self._msl = None

    def OnStarted2(self, time):
        super(mslea_strategy, self).OnStarted2(time)
        self._high_levels = []
        self._low_levels = []
        self._prev_high1 = None
        self._prev_high2 = None
        self._prev_low1 = None
        self._prev_low2 = None
        self._msh = None
        self._msl = None
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self._prev_high2 is not None and self._prev_high1 is not None:
            if self._prev_high2 < self._prev_high1 and self._prev_high1 > high:
                self._add_high(self._prev_high1)
        if self._prev_low2 is not None and self._prev_low1 is not None:
            if self._prev_low2 > self._prev_low1 and self._prev_low1 < low:
                self._add_low(self._prev_low1)
        self._prev_high2 = self._prev_high1
        self._prev_high1 = high
        self._prev_low2 = self._prev_low1
        self._prev_low1 = low
        if self._msh is not None and self._msl is not None:
            step = 0.01
            offset = step * self.distance
            upper = self._msh + offset
            lower = self._msl - offset
            close = float(candle.ClosePrice)
            if close > upper and self.Position <= 0:
                self.BuyMarket()
            elif close < lower and self.Position >= 0:
                self.SellMarket()

    def _add_high(self, high):
        self._high_levels.insert(0, high)
        while len(self._high_levels) > self.level:
            self._high_levels.pop()
        self._msh = max(self._high_levels)

    def _add_low(self, low):
        self._low_levels.insert(0, low)
        while len(self._low_levels) > self.level:
            self._low_levels.pop()
        self._msl = min(self._low_levels)

    def CreateClone(self):
        return mslea_strategy()
