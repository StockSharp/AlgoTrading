import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cross_ma_strategy(Strategy):
    def __init__(self):
        super(cross_ma_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5)
        self._slow_period = self.Param("SlowPeriod", 20)
        self._atr_period = self.Param("AtrPeriod", 6)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._last_signal = 0

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(cross_ma_strategy, self).OnStarted2(time)

        self._last_signal = 0

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastPeriod

        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast)
        slow_val = float(slow)
        close = float(candle.ClosePrice)

        if fast_val > slow_val and close > slow_val and self._last_signal != 1 and self.Position <= 0:
            self.BuyMarket()
            self._last_signal = 1
        elif fast_val < slow_val and close < slow_val and self._last_signal != -1 and self.Position >= 0:
            self.SellMarket()
            self._last_signal = -1

    def OnReseted(self):
        super(cross_ma_strategy, self).OnReseted()
        self._last_signal = 0

    def CreateClone(self):
        return cross_ma_strategy()
