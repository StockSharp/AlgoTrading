import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class blood_in_the_streets_strategy(Strategy):
    """
    Blood In The Streets strategy.
    The drawdown is the percent distance of the close below the highest high of the last LookbackPeriod candles. When it falls to
    its StdDevLength mean plus StdDevThreshold standard deviations or lower, the strategy buys, and it closes the long after ExitBars candles.
    """

    def __init__(self):
        super(blood_in_the_streets_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 50).SetGreaterThanZero().SetDisplay("Lookback Period", "Candles the highest high spans", "Indicators")
        self._std_dev_length = self.Param("StdDevLength", 50).SetGreaterThanZero().SetDisplay("StdDev Length", "Candles the drawdown mean and standard deviation span", "Indicators")
        self._std_dev_threshold = self.Param("StdDevThreshold", -1.0).SetDisplay("StdDev Threshold", "Standard deviations from the mean the drawdown has to reach", "Indicators")
        self._exit_bars = self.Param("ExitBars", 35).SetGreaterThanZero().SetDisplay("Exit Bars", "Candles a position is held", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._drawdown_mean = None
        self._drawdown_deviation = None
        self._bars_in_position = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(blood_in_the_streets_strategy, self).OnReseted()
        self._highest = None
        self._drawdown_mean = None
        self._drawdown_deviation = None
        self._bars_in_position = 0

    def OnStarted2(self, time):
        super(blood_in_the_streets_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._lookback_period.Value
        self._drawdown_mean = SimpleMovingAverage()
        self._drawdown_mean.Length = self._std_dev_length.Value
        self._drawdown_deviation = StandardDeviation()
        self._drawdown_deviation.Length = self._std_dev_length.Value
        self._bars_in_position = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process(self, indicator, value, time):
        indicator_input = DecimalIndicatorValue(indicator, value, time)
        indicator_input.IsFinal = True
        return indicator.Process(indicator_input)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        highest_value = self._process(self._highest, candle.HighPrice, candle.OpenTime)

        if not highest_value.IsFormed:
            return

        highest = highest_value.GetValue[Decimal](None)

        if highest <= 0:
            return

        drawdown = (candle.ClosePrice - highest) / highest * Decimal(100)
        mean_value = self._process(self._drawdown_mean, drawdown, candle.OpenTime)
        deviation_value = self._process(self._drawdown_deviation, drawdown, candle.OpenTime)

        if self.Position > 0:
            self._bars_in_position += 1

        if not mean_value.IsFormed or not deviation_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if self._bars_in_position >= self._exit_bars.Value:
                self.SellMarket(self.Position)
            return

        threshold = mean_value.GetValue[Decimal](None) + Decimal(self._std_dev_threshold.Value) * deviation_value.GetValue[Decimal](None)

        if self.Position == 0 and drawdown <= threshold:
            self.BuyMarket(self.Volume)
            self._bars_in_position = 0

    def CreateClone(self):
        return blood_in_the_streets_strategy()
