import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_cross_method_price_mode_strategy(Strategy):
    """
    MA crossover strategy with configurable periods.
    """

    def __init__(self):
        super(ma_cross_method_price_mode_strategy, self).__init__()
        self._first_period = self.Param("FirstPeriod", 3).SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._second_period = self.Param("SecondPeriod", 13).SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._first_values = []
        self._second_values = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_cross_method_price_mode_strategy, self).OnReseted()
        self._first_values = []
        self._second_values = []

    def OnStarted(self, time):
        super(ma_cross_method_price_mode_strategy, self).OnStarted(time)
        first = SimpleMovingAverage()
        first.Length = self._first_period.Value
        second = SimpleMovingAverage()
        second.Length = self._second_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(first, second, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, first)
            self.DrawIndicator(area, second)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, first_val, second_val):
        if candle.State != CandleStates.Finished:
            return
        f = float(first_val)
        s = float(second_val)
        self._first_values.append(f)
        self._second_values.append(s)
        if len(self._first_values) > 4:
            self._first_values.pop(0)
        if len(self._second_values) > 4:
            self._second_values.pop(0)
        if len(self._first_values) < 2:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        prev_f = self._first_values[-2]
        curr_f = self._first_values[-1]
        curr_s = self._second_values[-1]
        bull = prev_f <= curr_s and curr_f > curr_s
        bear = prev_f >= curr_s and curr_f < curr_s
        if bull and self.Position <= 0:
            self.BuyMarket()
        elif bear and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ma_cross_method_price_mode_strategy()
