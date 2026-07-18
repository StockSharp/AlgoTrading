import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ravi_histogram_strategy(Strategy):
    def __init__(self):
        super(ravi_histogram_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 7) \
            .SetDisplay("Fast Length", "Fast EMA length", "General")
        self._slow_length = self.Param("SlowLength", 65) \
            .SetDisplay("Slow Length", "Slow EMA length", "General")
        self._up_level = self.Param("UpLevel", 0.1) \
            .SetDisplay("Upper Level", "Upper threshold for trend", "General")
        self._down_level = self.Param("DownLevel", -0.1) \
            .SetDisplay("Lower Level", "Lower threshold for trend", "General")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Open Long", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Open Short", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Trading")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ravi = 0.0
        self._is_first = True

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def buy_open(self):
        return self._buy_open.Value

    @property
    def sell_open(self):
        return self._sell_open.Value

    @property
    def buy_close(self):
        return self._buy_close.Value

    @property
    def sell_close(self):
        return self._sell_close.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ravi_histogram_strategy, self).OnReseted()
        self._prev_ravi = 0.0
        self._is_first = True

    def OnStarted2(self, time):
        super(ravi_histogram_strategy, self).OnStarted2(time)
        self._is_first = True
        self._prev_ravi = 0.0
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_length
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        if slow == 0:
            return
        ravi = 100.0 * (fast - slow) / slow
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ravi = ravi
            self._is_first = False
            return
        if self._is_first:
            self._prev_ravi = ravi
            self._is_first = False
            return
        up_level = float(self.up_level)
        down_level = float(self.down_level)
        if ravi > up_level:
            if self.sell_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_open and self._prev_ravi <= up_level and self.Position <= 0:
                self.BuyMarket()
        elif ravi < down_level:
            if self.buy_close and self.Position > 0:
                self.SellMarket()
            if self.sell_open and self._prev_ravi >= down_level and self.Position >= 0:
                self.SellMarket()
        self._prev_ravi = ravi

    def CreateClone(self):
        return ravi_histogram_strategy()
