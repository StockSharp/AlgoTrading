import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class artificial_intelligence_strategy(Strategy):
    """
    Artificial Intelligence strategy based on a simple perceptron over Accelerator Oscillator values.
    """

    def __init__(self):
        super(artificial_intelligence_strategy, self).__init__()

        self._x1 = self.Param("X1", 135) \
            .SetDisplay("X1", "Perceptron weight 1", "Perceptron") \
            .SetOptimize(0, 200, 5)
        self._x2 = self.Param("X2", 127) \
            .SetDisplay("X2", "Perceptron weight 2", "Perceptron") \
            .SetOptimize(0, 200, 5)
        self._x3 = self.Param("X3", 16) \
            .SetDisplay("X3", "Perceptron weight 3", "Perceptron") \
            .SetOptimize(0, 200, 5)
        self._x4 = self.Param("X4", 93) \
            .SetDisplay("X4", "Perceptron weight 4", "Perceptron") \
            .SetOptimize(0, 200, 5)
        self._stop_loss = self.Param("StopLoss", 85.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in points", "Risk") \
            .SetOptimize(10.0, 200.0, 5.0)
        self._candle_type = self.Param("CandleType", tf(240)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._ao_buffer = [0.0] * 22
        self._ao_count = 0
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def X1(self): return self._x1.Value
    @X1.setter
    def X1(self, v): self._x1.Value = v
    @property
    def X2(self): return self._x2.Value
    @X2.setter
    def X2(self, v): self._x2.Value = v
    @property
    def X3(self): return self._x3.Value
    @X3.setter
    def X3(self, v): self._x3.Value = v
    @property
    def X4(self): return self._x4.Value
    @X4.setter
    def X4(self, v): self._x4.Value = v
    @property
    def StopLoss(self): return self._stop_loss.Value
    @StopLoss.setter
    def StopLoss(self, v): self._stop_loss.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(artificial_intelligence_strategy, self).OnReseted()
        self._ao_buffer = [0.0] * 22
        self._ao_count = 0
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(artificial_intelligence_strategy, self).OnStarted(time)

        ao = AwesomeOscillator()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ao, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ao)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ao_value):
        if candle.State != CandleStates.Finished:
            return

        # Shift buffer
        for i in range(len(self._ao_buffer) - 1, 0, -1):
            self._ao_buffer[i] = self._ao_buffer[i - 1]
        self._ao_buffer[0] = float(ao_value)
        if self._ao_count < len(self._ao_buffer):
            self._ao_count += 1

        if self._ao_count < len(self._ao_buffer):
            return

        step = float(self.Security.PriceStep or 0.01)
        close = float(candle.ClosePrice)

        w1 = self.X1 - 100.0
        w2 = self.X2 - 100.0
        w3 = self.X3 - 100.0
        w4 = self.X4 - 100.0

        perceptron = w1 * self._ao_buffer[0] + w2 * self._ao_buffer[7] + w3 * self._ao_buffer[14] + w4 * self._ao_buffer[21]

        if self.Position == 0:
            if perceptron > 0:
                self._entry_price = close
                self._stop_price = self._entry_price - self.StopLoss * step
                self.BuyMarket()
            else:
                self._entry_price = close
                self._stop_price = self._entry_price + self.StopLoss * step
                self.SellMarket()
            return

        if self.Position > 0:
            self._stop_price = max(self._stop_price, close - self.StopLoss * step)
            if close <= self._stop_price or perceptron < 0:
                self.SellMarket()
                if perceptron < 0:
                    self._entry_price = close
                    self._stop_price = self._entry_price + self.StopLoss * step
        elif self.Position < 0:
            self._stop_price = min(self._stop_price, close + self.StopLoss * step)
            if close >= self._stop_price or perceptron > 0:
                self.BuyMarket()
                if perceptron > 0:
                    self._entry_price = close
                    self._stop_price = self._entry_price - self.StopLoss * step

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return artificial_intelligence_strategy()
