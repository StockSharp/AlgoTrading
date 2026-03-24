import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class wodisma_triple_ma_crossover_strategy(Strategy):
    def __init__(self):
        super(wodisma_triple_ma_crossover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast MA", "Fast MA period", "MA")
        self._mid_length = self.Param("MidLength", 20) \
            .SetDisplay("Mid MA", "Middle MA period", "MA")
        self._slow_length = self.Param("SlowLength", 50) \
            .SetDisplay("Slow MA", "Slow MA period", "MA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def mid_length(self):
        return self._mid_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(wodisma_triple_ma_crossover_strategy, self).OnStarted(time)
        fast = SimpleMovingAverage()
        fast.Length = self.fast_length
        mid = SimpleMovingAverage()
        mid.Length = self.mid_length
        slow = SimpleMovingAverage()
        slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, mid, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, mid)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, mid, slow):
        if candle.State != CandleStates.Finished:
            return
        if fast > mid and mid > slow and self.Position <= 0:
            self.BuyMarket()
        elif fast < mid and mid < slow and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return wodisma_triple_ma_crossover_strategy()
