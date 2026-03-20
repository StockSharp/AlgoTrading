import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class color_xva_ma_digit_st_dev_strategy(Strategy):
    def __init__(self):
        super(color_xva_ma_digit_st_dev_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 15) \
            .SetDisplay("EMA Length", "Period for the exponential moving average", "Parameters")
        self._std_length = self.Param("StdLength", 9) \
            .SetDisplay("StdDev Length", "Period for standard deviation", "Parameters")
        self._k1 = self.Param("K1", 1.5) \
            .SetDisplay("Deviation K1", "Inner band multiplier", "Parameters")
        self._k2 = self.Param("K2", 2.5) \
            .SetDisplay("Deviation K2", "Outer band multiplier", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for market data", "General")

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def std_length(self):
        return self._std_length.Value

    @property
    def k1(self):
        return self._k1.Value

    @property
    def k2(self):
        return self._k2.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(color_xva_ma_digit_st_dev_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = int(self.ma_length)
        std = StandardDeviation()
        std.Length = int(self.std_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, std, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)

    def process_candle(self, candle, ema_value, std_value):
        if candle.State != CandleStates.Finished:
            return
        ema_value = float(ema_value)
        std_value = float(std_value)
        if std_value == 0:
            return
        close = float(candle.ClosePrice)
        deviation = close - ema_value
        k1 = float(self.k1)
        k2 = float(self.k2)
        filter1 = k1 * std_value
        filter2 = k2 * std_value
        if self.Position <= 0 and deviation > filter2:
            self.BuyMarket()
        elif self.Position >= 0 and deviation < -filter2:
            self.SellMarket()
        elif self.Position > 0 and deviation < filter1:
            self.SellMarket()
        elif self.Position < 0 and deviation > -filter1:
            self.BuyMarket()

    def CreateClone(self):
        return color_xva_ma_digit_st_dev_strategy()
