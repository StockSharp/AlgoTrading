import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class batman_atr_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(batman_atr_trailing_stop_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR indicator period", "General")
        self._factor = self.Param("Factor", 1.5) \
            .SetDisplay("ATR Factor", "Multiplier for ATR distance", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._level_up = 0.0
        self._level_down = 0.0
        self._direction = 1
        self._is_initialized = False

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def factor(self):
        return self._factor.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(batman_atr_trailing_stop_strategy, self).OnReseted()
        self._level_up = 0.0
        self._level_down = 0.0
        self._direction = 1
        self._is_initialized = False

    def OnStarted2(self, time):
        super(batman_atr_trailing_stop_strategy, self).OnStarted2(time)
        stdev = StandardDeviation()
        stdev.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(stdev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, stdev_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        curr_up = close - stdev_value * self.factor
        curr_down = close + stdev_value * self.factor
        if not self._is_initialized:
            self._level_up = curr_up
            self._level_down = curr_down
            self._is_initialized = True
            return
        if self._direction == 1:
            if curr_up > self._level_up:
                self._level_up = curr_up
            if candle.LowPrice < self._level_up:
                self._direction = -1
                self._level_down = curr_down
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        else:
            if curr_down < self._level_down:
                self._level_down = curr_down
            if candle.HighPrice > self._level_down:
                self._direction = 1
                self._level_up = curr_up
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()

    def CreateClone(self):
        return batman_atr_trailing_stop_strategy()
