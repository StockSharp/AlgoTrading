import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_j2_jma_std_dev_strategy(Strategy):
    def __init__(self):
        super(color_j2_jma_std_dev_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "Period of JMA", "Parameters")
        self._std_dev_period = self.Param("StdDevPeriod", 9) \
            .SetDisplay("StdDev Period", "Period of standard deviation", "Parameters")
        self._k1 = self.Param("K1", 0.5) \
            .SetDisplay("K1", "First threshold multiplier (close)", "Parameters")
        self._k2 = self.Param("K2", 1.0) \
            .SetDisplay("K2", "Second threshold multiplier (entry)", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "Parameters")
        self._prev_jma = None
        self._std_dev = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def std_dev_period(self):
        return self._std_dev_period.Value

    @property
    def k1(self):
        return self._k1.Value

    @property
    def k2(self):
        return self._k2.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_j2_jma_std_dev_strategy, self).OnReseted()
        self._prev_jma = None
        self._std_dev = None

    def OnStarted(self, time):
        super(color_j2_jma_std_dev_strategy, self).OnStarted(time)
        self._prev_jma = None
        jma = JurikMovingAverage()
        jma.Length = self.jma_length
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.std_dev_period
        self.Indicators.Add(self._std_dev)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        if self._prev_jma is None:
            self._prev_jma = jma_value
            return
        diff = jma_value - self._prev_jma
        self._prev_jma = jma_value
        input_val = DecimalIndicatorValue(self._std_dev, diff, candle.ServerTime)
        input_val.IsFinal = True
        std_result = self._std_dev.Process(input_val)
        if not self._std_dev.IsFormed:
            return
        st_dev = float(std_result)
        if st_dev == 0:
            return
        k1_val = float(self.k1)
        k2_val = float(self.k2)
        low_threshold = k1_val * st_dev
        high_threshold = k2_val * st_dev
        if self.Position > 0 and diff < -low_threshold:
            self.SellMarket()
            return
        if self.Position < 0 and diff > low_threshold:
            self.BuyMarket()
            return
        if self.Position <= 0 and diff > high_threshold:
            self.BuyMarket()
        elif self.Position >= 0 and diff < -high_threshold:
            self.SellMarket()

    def CreateClone(self):
        return color_j2_jma_std_dev_strategy()
