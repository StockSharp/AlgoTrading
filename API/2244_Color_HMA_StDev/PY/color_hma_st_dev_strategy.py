import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class color_hma_st_dev_strategy(Strategy):
    def __init__(self):
        super(color_hma_st_dev_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 13) \
            .SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
        self._std_period = self.Param("StdPeriod", 9) \
            .SetDisplay("StdDev Period", "Standard deviation period", "Indicators")
        self._k1 = self.Param("K1", 0.5) \
            .SetDisplay("Entry Multiplier", "Deviation multiplier for entry", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to subscribe", "Common")

    @property
    def hma_period(self):
        return self._hma_period.Value

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def k1(self):
        return self._k1.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(color_hma_st_dev_strategy, self).OnStarted(time)
        hma = HullMovingAverage()
        hma.Length = self.hma_period
        std = StandardDeviation()
        std.Length = self.std_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, std, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, hma_value, std_value):
        if candle.State != CandleStates.Finished:
            return
        hma_value = float(hma_value)
        std_value = float(std_value)
        if std_value == 0:
            return
        k1_val = float(self.k1)
        upper_entry = hma_value + k1_val * std_value
        lower_entry = hma_value - k1_val * std_value
        close_price = float(candle.ClosePrice)
        if close_price > upper_entry and self.Position <= 0:
            self.BuyMarket()
        elif close_price < lower_entry and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return color_hma_st_dev_strategy()
