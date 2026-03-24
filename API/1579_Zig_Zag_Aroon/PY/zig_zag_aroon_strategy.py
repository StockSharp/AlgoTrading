import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zig_zag_aroon_strategy(Strategy):
    def __init__(self):
        super(zig_zag_aroon_strategy, self).__init__()
        self._zig_zag_depth = self.Param("ZigZagDepth", 5) \
            .SetDisplay("ZigZag Depth", "Pivot search depth", "ZigZag")
        self._aroon_length = self.Param("AroonLength", 14) \
            .SetDisplay("Aroon Period", "Aroon indicator period", "Aroon")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._highs = []
        self._lows = []
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._direction = 0
        self._prev_aroon_up = 0.0
        self._prev_aroon_down = 0.0

    @property
    def zig_zag_depth(self):
        return self._zig_zag_depth.Value

    @property
    def aroon_length(self):
        return self._aroon_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zig_zag_aroon_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._direction = 0
        self._prev_aroon_up = 0.0
        self._prev_aroon_down = 0.0

    def OnStarted(self, time):
        super(zig_zag_aroon_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)
        max_len = max(int(self.zig_zag_depth), int(self.aroon_length)) + 2
        if len(self._highs) > max_len * 2:
            self._highs.pop(0)
            self._lows.pop(0)
        if len(self._highs) < int(self.zig_zag_depth):
            return
        # Manual highest/lowest over ZigZagDepth
        depth = int(self.zig_zag_depth)
        recent_highs = self._highs[-depth:]
        recent_lows = self._lows[-depth:]
        highest = max(recent_highs)
        lowest = min(recent_lows)
        # ZigZag direction
        if candle.HighPrice >= highest and self._direction != 1:
            self._last_zigzag_high = candle.HighPrice
            self._direction = 1
        elif candle.LowPrice <= lowest and self._direction != -1:
            self._last_zigzag_low = candle.LowPrice
            self._direction = -1
        al = int(self.aroon_length)
        if len(self._highs) < al + 1:
            return
        # Manual Aroon calculation
        count = al + 1
        if len(self._highs) < count or len(self._lows) < count:
            return
        aroon_highs = self._highs[-count:]
        aroon_lows = self._lows[-count:]
        highest_idx = 0
        lowest_idx = 0
        for i in range(1, count):
            if aroon_highs[i] >= aroon_highs[highest_idx]:
                highest_idx = i
            if aroon_lows[i] <= aroon_lows[lowest_idx]:
                lowest_idx = i
        aroon_up = 100.0 * highest_idx / al
        aroon_down = 100.0 * lowest_idx / al
        # Aroon crossover
        cross_up = self._prev_aroon_up <= self._prev_aroon_down and aroon_up > aroon_down
        cross_down = self._prev_aroon_down <= self._prev_aroon_up and aroon_down > aroon_up
        if cross_up and self._direction == 1 and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self._direction == -1 and self.Position >= 0:
            self.SellMarket()
        self._prev_aroon_up = aroon_up
        self._prev_aroon_down = aroon_down

    def CreateClone(self):
        return zig_zag_aroon_strategy()
