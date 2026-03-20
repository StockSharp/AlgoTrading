import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class pr_ophet_strategy(Strategy):
    def __init__(self):
        super(pr_ophet_strategy, self).__init__()

        self._x1 = self.Param("X1", 9)
        self._x2 = self.Param("X2", 29)
        self._x3 = self.Param("X3", 94)
        self._x4 = self.Param("X4", 125)
        self._y1 = self.Param("Y1", 61)
        self._y2 = self.Param("Y2", 100)
        self._y3 = self.Param("Y3", 117)
        self._y4 = self.Param("Y4", 31)
        self._stop_multiplier = self.Param("StopMultiplier", 0.005)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._stop_price = 0.0
        self._prev_high1 = 0.0
        self._prev_low1 = 0.0
        self._prev_high2 = 0.0
        self._prev_low2 = 0.0
        self._prev_high3 = 0.0
        self._prev_low3 = 0.0
        self._history_count = 0

    @property
    def X1(self):
        return self._x1.Value

    @X1.setter
    def X1(self, value):
        self._x1.Value = value

    @property
    def X2(self):
        return self._x2.Value

    @X2.setter
    def X2(self, value):
        self._x2.Value = value

    @property
    def X3(self):
        return self._x3.Value

    @X3.setter
    def X3(self, value):
        self._x3.Value = value

    @property
    def X4(self):
        return self._x4.Value

    @X4.setter
    def X4(self, value):
        self._x4.Value = value

    @property
    def Y1(self):
        return self._y1.Value

    @Y1.setter
    def Y1(self, value):
        self._y1.Value = value

    @property
    def Y2(self):
        return self._y2.Value

    @Y2.setter
    def Y2(self, value):
        self._y2.Value = value

    @property
    def Y3(self):
        return self._y3.Value

    @Y3.setter
    def Y3(self, value):
        self._y3.Value = value

    @property
    def Y4(self):
        return self._y4.Value

    @Y4.setter
    def Y4(self, value):
        self._y4.Value = value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stop_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(pr_ophet_strategy, self).OnStarted(time)

        self._history_count = 0
        self._stop_price = 0.0
        self._prev_high1 = 0.0
        self._prev_low1 = 0.0
        self._prev_high2 = 0.0
        self._prev_low2 = 0.0
        self._prev_high3 = 0.0
        self._prev_low3 = 0.0

        atr = AverageTrueRange()
        atr.Length = 14

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def _qu(self, q1, q2, q3, q4):
        return ((q1 - 100) * abs(self._prev_high1 - self._prev_low2)
              + (q2 - 100) * abs(self._prev_high3 - self._prev_low2)
              + (q3 - 100) * abs(self._prev_high2 - self._prev_low1)
              + (q4 - 100) * abs(self._prev_high2 - self._prev_low3))

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        atr_val = float(atr_value)

        if self._history_count >= 3:
            if self.Position > 0:
                new_stop = close - atr_val * 2.0
                if new_stop > self._stop_price:
                    self._stop_price = new_stop
                if close <= self._stop_price:
                    self.SellMarket()
            elif self.Position < 0:
                new_stop = close + atr_val * 2.0
                if new_stop < self._stop_price or self._stop_price == 0.0:
                    self._stop_price = new_stop
                if close >= self._stop_price:
                    self.BuyMarket()
            else:
                buy_signal = self._qu(int(self.X1), int(self.X2), int(self.X3), int(self.X4))
                sell_signal = self._qu(int(self.Y1), int(self.Y2), int(self.Y3), int(self.Y4))

                if buy_signal > 0.0 and sell_signal <= 0.0:
                    self._stop_price = close - atr_val * 2.0
                    self.BuyMarket()
                elif sell_signal > 0.0 and buy_signal <= 0.0:
                    self._stop_price = close + atr_val * 2.0
                    self.SellMarket()

        self._update_history(candle)

    def _update_history(self, candle):
        self._prev_high3 = self._prev_high2
        self._prev_low3 = self._prev_low2
        self._prev_high2 = self._prev_high1
        self._prev_low2 = self._prev_low1
        self._prev_high1 = float(candle.HighPrice)
        self._prev_low1 = float(candle.LowPrice)

        if self._history_count < 3:
            self._history_count += 1

    def OnReseted(self):
        super(pr_ophet_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._prev_high1 = 0.0
        self._prev_low1 = 0.0
        self._prev_high2 = 0.0
        self._prev_low2 = 0.0
        self._prev_high3 = 0.0
        self._prev_low3 = 0.0
        self._history_count = 0

    def CreateClone(self):
        return pr_ophet_strategy()
