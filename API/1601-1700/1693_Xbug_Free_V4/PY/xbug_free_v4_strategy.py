import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class xbug_free_v4_strategy(Strategy):
    def __init__(self):
        super(xbug_free_v4_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("MA Period", "Moving average length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_sma = 0.0
        self._prev_price = 0.0
        self._prev2_sma = 0.0
        self._prev2_price = 0.0
        self._bar_count = 0

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xbug_free_v4_strategy, self).OnReseted()
        self._prev_sma = 0.0
        self._prev_price = 0.0
        self._prev2_sma = 0.0
        self._prev2_price = 0.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(xbug_free_v4_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        stdev = StandardDeviation()
        stdev.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, stdev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_value, stdev_value):
        if candle.State != CandleStates.Finished:
            return
        median = (candle.HighPrice + candle.LowPrice) / 2
        self._bar_count += 1
        if self._bar_count >= 3:
            buy_signal = sma_value > median and self._prev_sma > self._prev_price and self._prev2_sma < self._prev2_price
            sell_signal = sma_value < median and self._prev_sma < self._prev_price and self._prev2_sma > self._prev2_price
            if buy_signal and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif sell_signal and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev2_sma = self._prev_sma
        self._prev2_price = self._prev_price
        self._prev_sma = sma_value
        self._prev_price = median

    def CreateClone(self):
        return xbug_free_v4_strategy()
