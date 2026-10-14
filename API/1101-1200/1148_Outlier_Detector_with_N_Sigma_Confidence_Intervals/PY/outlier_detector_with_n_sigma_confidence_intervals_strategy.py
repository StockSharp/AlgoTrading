import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class outlier_detector_with_n_sigma_confidence_intervals_strategy(Strategy):
    """
    Outlier detector with N-sigma confidence intervals.
    The close-to-close price change is turned into a z-score against the mean and standard deviation of the last SampleSize changes.
    A z-score above SecondLimit goes short and one below -SecondLimit goes long, reversing an opposite position.
    The position is closed once the absolute z-score falls back below FirstLimit.
    """

    def __init__(self):
        super(outlier_detector_with_n_sigma_confidence_intervals_strategy, self).__init__()
        self._sample_size = self.Param("SampleSize", 30).SetGreaterThanZero().SetDisplay("Sample Size", "Number of price changes in the sample", "Indicators")
        self._first_limit = self.Param("FirstLimit", 2.0).SetGreaterThanZero().SetDisplay("First Limit", "Z-score below which the position is closed", "Signals")
        self._second_limit = self.Param("SecondLimit", 3.0).SetGreaterThanZero().SetDisplay("Second Limit", "Z-score beyond which a move counts as an outlier", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._mean = None
        self._std_dev = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(outlier_detector_with_n_sigma_confidence_intervals_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted2(self, time):
        super(outlier_detector_with_n_sigma_confidence_intervals_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._mean = SimpleMovingAverage()
        self._mean.Length = self._sample_size.Value
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self._sample_size.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        prev_close = self._prev_close
        self._prev_close = candle.ClosePrice

        if prev_close is None:
            return

        change = candle.ClosePrice - prev_close

        mean_input = DecimalIndicatorValue(self._mean, change, candle.OpenTime)
        mean_input.IsFinal = True
        mean_value = self._mean.Process(mean_input)

        std_input = DecimalIndicatorValue(self._std_dev, change, candle.OpenTime)
        std_input.IsFinal = True
        std_value = self._std_dev.Process(std_input)

        if not self._mean.IsFormed or not self._std_dev.IsFormed:
            return

        std = std_value.GetValue[Decimal](None)
        if std <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        z = (change - mean_value.GetValue[Decimal](None)) / std
        second = Decimal(self._second_limit.Value)
        first = Decimal(self._first_limit.Value)

        if z > second and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif z < -second and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position != 0 and abs(z) < first:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return outlier_detector_with_n_sigma_confidence_intervals_strategy()
