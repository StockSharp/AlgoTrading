import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_xdin_ma_st_dev_strategy(Strategy):
    def __init__(self):
        super(color_xdin_ma_st_dev_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Type of candles", "General")
        self._main_length = self.Param("MainLength", 10) \
            .SetDisplay("Main MA Length", "Length of primary moving average", "Parameters")
        self._plus_length = self.Param("PlusLength", 20) \
            .SetDisplay("Plus MA Length", "Length of secondary moving average", "Parameters")
        self._std_period = self.Param("StdPeriod", 9) \
            .SetDisplay("StdDev Period", "Period for standard deviation of MA changes", "Parameters")
        self._k1 = self.Param("K1", 0.5) \
            .SetDisplay("Filter K1", "Multiplier for standard deviation filter", "Parameters")
        self._std_dev = None
        self._prev_xdin = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def main_length(self):
        return self._main_length.Value

    @property
    def plus_length(self):
        return self._plus_length.Value

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def k1(self):
        return self._k1.Value

    def OnReseted(self):
        super(color_xdin_ma_st_dev_strategy, self).OnReseted()
        self._prev_xdin = None
        self._std_dev = None

    def OnStarted2(self, time):
        super(color_xdin_ma_st_dev_strategy, self).OnStarted2(time)
        self._prev_xdin = None
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.std_period
        self.Indicators.Add(self._std_dev)
        main_ma = ExponentialMovingAverage()
        main_ma.Length = self.main_length
        plus_ma = ExponentialMovingAverage()
        plus_ma.Length = self.plus_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(main_ma, plus_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, main_ma)
            self.DrawIndicator(area, plus_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, main_value, plus_value):
        if candle.State != CandleStates.Finished:
            return
        main_value = float(main_value)
        plus_value = float(plus_value)
        xdin = main_value * 2.0 - plus_value
        if self._prev_xdin is None:
            self._prev_xdin = xdin
            return
        change = xdin - self._prev_xdin
        self._prev_xdin = xdin
        input_val = DecimalIndicatorValue(self._std_dev, change, candle.ServerTime)
        input_val.IsFinal = True
        std_result = self._std_dev.Process(input_val)
        if not self._std_dev.IsFormed:
            return
        st_dev = float(std_result)
        if st_dev == 0:
            return
        filter_val = float(self.k1) * st_dev
        if change > filter_val and self.Position <= 0:
            self.BuyMarket()
        elif change < -filter_val and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return color_xdin_ma_st_dev_strategy()
