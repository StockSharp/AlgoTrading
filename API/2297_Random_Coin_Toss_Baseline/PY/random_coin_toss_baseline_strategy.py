import clr
import random as py_random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class random_coin_toss_baseline_strategy(Strategy):
    def __init__(self):
        super(random_coin_toss_baseline_strategy, self).__init__()
        self._hold_bars = self.Param("HoldBars", 10) \
            .SetDisplay("Hold Bars", "Number of bars to hold position", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._random = None
        self._bars_in_position = 0

    @property
    def hold_bars(self):
        return self._hold_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(random_coin_toss_baseline_strategy, self).OnReseted()
        self._random = None
        self._bars_in_position = 0

    def OnStarted(self, time):
        super(random_coin_toss_baseline_strategy, self).OnStarted(time)
        self._random = py_random.Random(42)
        self._bars_in_position = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self.Position != 0:
            self._bars_in_position += 1
            if self._bars_in_position >= int(self.hold_bars):
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._bars_in_position = 0
            return
        coin = self._random.randint(0, 1)
        if coin == 0:
            self.BuyMarket()
        else:
            self.SellMarket()
        self._bars_in_position = 0

    def CreateClone(self):
        return random_coin_toss_baseline_strategy()
