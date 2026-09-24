import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class artificial_intelligence_perceptron_strategy(Strategy):
    def __init__(self):
        super(artificial_intelligence_perceptron_strategy, self).__init__()

        self._stop_loss = self.Param("StopLoss", 850.0).SetGreaterThanZero()
        self._shift = self.Param("Shift", 1).SetNotNegative()
        self._x1 = self.Param("X1", 1.0)
        self._x2 = self.Param("X2", 1.0)
        self._x3 = self.Param("X3", 1.0)
        self._x4 = self.Param("X4", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._median = []
        self._ao = []
        self._ac = []
        self._entry_price = 0.0
        self._stop_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(artificial_intelligence_perceptron_strategy, self).OnReseted()
        self._median = []
        self._ao = []
        self._ac = []
        self._entry_price = 0.0
        self._stop_price = None

    def OnStarted2(self, time):
        super(artificial_intelligence_perceptron_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        self._median.append(median)

        if len(self._median) < 34:
            return

        ao = self._avg_tail(self._median, 5) - self._avg_tail(self._median, 34)
        self._ao.append(ao)

        if len(self._ao) < 5:
            return

        ac = ao - self._avg_tail(self._ao, 5)
        self._ac.append(ac)

        self._apply_stop(candle)

        newest = len(self._ac) - 1 - int(self._shift.Value)
        if newest - 21 < 0:
            return

        output = (
            float(self._x1.Value) * self._ac[newest] +
            float(self._x2.Value) * self._ac[newest - 7] +
            float(self._x3.Value) * self._ac[newest - 14] +
            float(self._x4.Value) * self._ac[newest - 21]
        )

        signal = 1 if output > 0 else (-1 if output < 0 else 0)
        if signal == 0:
            return

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0
        stop_distance = float(self._stop_loss.Value) * step
        close = float(candle.ClosePrice)

        if self.Position == 0:
            self._enter(signal, float(self.Volume), close, stop_distance)
            return

        current_direction = 1 if self.Position > 0 else -1
        if signal == current_direction:
            return

        profit_distance = close - self._entry_price if current_direction > 0 else self._entry_price - close

        if profit_distance > stop_distance * 2.0:
            order_volume = abs(float(self.Position)) + float(self.Volume) * 2.0
            if signal > 0:
                self.BuyMarket(order_volume)
            else:
                self.SellMarket(order_volume)
            self._entry_price = close
            self._stop_price = close - stop_distance if signal > 0 else close + stop_distance
        else:
            self._stop_price = self._entry_price

    def _apply_stop(self, candle):
        if self.Position > 0 and self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._stop_price = None
        elif self.Position < 0 and self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._stop_price = None

    def _enter(self, signal, volume, price, stop_distance):
        if signal > 0:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)
        self._entry_price = price
        self._stop_price = price - stop_distance if signal > 0 else price + stop_distance

    @staticmethod
    def _avg_tail(values, count):
        return sum(values[-count:]) / float(count)

    def CreateClone(self):
        return artificial_intelligence_perceptron_strategy()
