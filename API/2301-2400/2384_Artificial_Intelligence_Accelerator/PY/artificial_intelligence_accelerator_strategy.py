import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class artificial_intelligence_accelerator_strategy(Strategy):
    def __init__(self):
        super(artificial_intelligence_accelerator_strategy, self).__init__()

        self._x1 = self.Param("X1", 76)
        self._x2 = self.Param("X2", 47)
        self._x3 = self.Param("X3", 153)
        self._x4 = self.Param("X4", 135)
        self._stop_loss = self.Param("StopLoss", 8355.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._ac_buffer = [0.0] * 22
        self._buffer_count = 0
        self._entry_price = 0.0
        self._prev_signal = None
        self._bars_since_trade = 10

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
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(artificial_intelligence_accelerator_strategy, self).OnStarted2(time)

        self._ac_buffer = [0.0] * 22
        self._buffer_count = 0
        self._entry_price = 0.0
        self._prev_signal = None
        self._bars_since_trade = 10

        self._ao_fast = SimpleMovingAverage()
        self._ao_fast.Length = 5
        self._ao_slow = SimpleMovingAverage()
        self._ao_slow.Length = 34
        self._ac_ma = SimpleMovingAverage()
        self._ac_ma.Length = 5

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_trade += 1

        hl2 = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        t = candle.OpenTime

        ao_fast_result = process_float(self._ao_fast, hl2, t, True)
        ao_slow_result = process_float(self._ao_slow, hl2, t, True)
        if not self._ao_fast.IsFormed or not self._ao_slow.IsFormed:
            return

        ao = float(ao_fast_result) - float(ao_slow_result)

        ac_ma_result = process_float(self._ac_ma, ao, t, True)
        if not self._ac_ma.IsFormed:
            return

        ac = ao - float(ac_ma_result)

        for i in range(21, 0, -1):
            self._ac_buffer[i] = self._ac_buffer[i - 1]
        self._ac_buffer[0] = ac

        if self._buffer_count < 22:
            self._buffer_count += 1
            return

        signal = self._perceptron()
        previous_signal = self._prev_signal
        self._prev_signal = signal

        if previous_signal is None:
            return

        close = float(candle.ClosePrice)

        if self._bars_since_trade >= 5 and previous_signal <= 0.0 and signal > 0.0 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._bars_since_trade = 0
        elif self._bars_since_trade >= 5 and previous_signal >= 0.0 and signal < 0.0 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._bars_since_trade = 0
        elif self.Position > 0 and close <= self._entry_price - float(self.StopLoss):
            self.SellMarket()
            self._bars_since_trade = 0
        elif self.Position < 0 and close >= self._entry_price + float(self.StopLoss):
            self.BuyMarket()
            self._bars_since_trade = 0

    def _perceptron(self):
        w1 = float(self.X1) - 100.0
        w2 = float(self.X2) - 100.0
        w3 = float(self.X3) - 100.0
        w4 = float(self.X4) - 100.0
        return w1 * self._ac_buffer[0] + w2 * self._ac_buffer[7] + w3 * self._ac_buffer[14] + w4 * self._ac_buffer[21]

    def OnReseted(self):
        super(artificial_intelligence_accelerator_strategy, self).OnReseted()
        self._ac_buffer = [0.0] * 22
        self._buffer_count = 0
        self._entry_price = 0.0
        self._prev_signal = None
        self._bars_since_trade = 10

    def CreateClone(self):
        return artificial_intelligence_accelerator_strategy()
