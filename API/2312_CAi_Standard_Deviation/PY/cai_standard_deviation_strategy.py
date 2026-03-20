import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class cai_standard_deviation_strategy(Strategy):
    def __init__(self):
        super(cai_standard_deviation_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 12) \
            .SetDisplay("MA Length", "Moving average length", "Parameters")
        self._std_dev_period = self.Param("StdDevPeriod", 9) \
            .SetDisplay("StdDev Period", "Standard deviation period", "Parameters")
        self._open_multiplier = self.Param("OpenMultiplier", 2.5) \
            .SetDisplay("Open Multiplier", "StdDev multiplier for entries", "Parameters")
        self._close_multiplier = self.Param("CloseMultiplier", 1.5) \
            .SetDisplay("Close Multiplier", "StdDev multiplier for exits", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used", "Parameters")

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def std_dev_period(self):
        return self._std_dev_period.Value

    @property
    def open_multiplier(self):
        return self._open_multiplier.Value

    @property
    def close_multiplier(self):
        return self._close_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(cai_standard_deviation_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.ma_length
        std_dev = StandardDeviation()
        std_dev.Length = self.std_dev_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, std_dev)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value, std_dev_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        sma_value = float(sma_value)
        std_dev_value = float(std_dev_value)
        open_mult = float(self.open_multiplier)
        close_mult = float(self.close_multiplier)
        upper_open = sma_value + open_mult * std_dev_value
        lower_open = sma_value - open_mult * std_dev_value
        upper_close = sma_value + close_mult * std_dev_value
        lower_close = sma_value - close_mult * std_dev_value
        close_price = float(candle.ClosePrice)
        if self.Position <= 0 and close_price > upper_open:
            self.BuyMarket()
        if self.Position >= 0 and close_price < lower_open:
            self.SellMarket()
        if self.Position > 0 and close_price < upper_close:
            self.SellMarket()
        if self.Position < 0 and close_price > lower_close:
            self.BuyMarket()

    def CreateClone(self):
        return cai_standard_deviation_strategy()
