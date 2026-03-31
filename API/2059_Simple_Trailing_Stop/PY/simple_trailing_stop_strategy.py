import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_trailing_stop_strategy(Strategy):

    def __init__(self):
        super(simple_trailing_stop_strategy, self).__init__()

        self._trail_percent = self.Param("TrailPercent", 2.0) \
            .SetDisplay("Trail %", "Trailing stop distance as percentage", "Protection")
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def TrailPercent(self):
        return self._trail_percent.Value

    @TrailPercent.setter
    def TrailPercent(self, value):
        self._trail_percent.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(simple_trailing_stop_strategy, self).OnStarted2(time)

        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(float(self.TrailPercent), UnitTypes.Percent),
            isStopTrailing=True
        )

        fast = ExponentialMovingAverage()
        fast.Length = self.FastLength
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowLength

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast, slow, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast_f = float(fast_val)
        slow_f = float(slow_val)

        if fast_f > slow_f and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif fast_f < slow_f and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def OnReseted(self):
        super(simple_trailing_stop_strategy, self).OnReseted()

    def CreateClone(self):
        return simple_trailing_stop_strategy()
