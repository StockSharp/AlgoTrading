import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x_trader_strategy(Strategy):
    def __init__(self):
        super(x_trader_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma1_period = self.Param("Ma1Period", 16) \
            .SetDisplay("MA1 Period", "Period of the first moving average", "Parameters")
        self._ma2_period = self.Param("Ma2Period", 10) \
            .SetDisplay("MA2 Period", "Period of the second moving average", "Parameters")
        self._ma1_prev = 0.0
        self._ma1_prev2 = 0.0
        self._ma2_prev = 0.0
        self._ma2_prev2 = 0.0
        self._has_prev2 = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma1_period(self):
        return self._ma1_period.Value

    @property
    def ma2_period(self):
        return self._ma2_period.Value

    def OnReseted(self):
        super(x_trader_strategy, self).OnReseted()
        self._ma1_prev = 0.0
        self._ma1_prev2 = 0.0
        self._ma2_prev = 0.0
        self._ma2_prev2 = 0.0
        self._has_prev2 = False

    def OnStarted(self, time):
        super(x_trader_strategy, self).OnStarted(time)
        ma1 = ExponentialMovingAverage()
        ma1.Length = self.ma1_period
        ma2 = ExponentialMovingAverage()
        ma2.Length = self.ma2_period
        self.SubscribeCandles(self.candle_type).Bind(ma1, ma2, self.process_candle).Start()

    def process_candle(self, candle, ma1_value, ma2_value):
        if candle.State != CandleStates.Finished:
            return

        m1 = float(ma1_value)
        m2 = float(ma2_value)

        if self._ma1_prev == 0.0:
            self._ma1_prev = m1
            self._ma2_prev = m2
            return

        if not self._has_prev2:
            self._ma1_prev2 = self._ma1_prev
            self._ma2_prev2 = self._ma2_prev
            self._ma1_prev = m1
            self._ma2_prev = m2
            self._has_prev2 = True
            return

        sell_signal = m1 > m2 and self._ma1_prev > self._ma2_prev and self._ma1_prev2 < self._ma2_prev2
        buy_signal = m1 < m2 and self._ma1_prev < self._ma2_prev and self._ma1_prev2 > self._ma2_prev2

        if sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        self._ma1_prev2 = self._ma1_prev
        self._ma2_prev2 = self._ma2_prev
        self._ma1_prev = m1
        self._ma2_prev = m2

    def CreateClone(self):
        return x_trader_strategy()
